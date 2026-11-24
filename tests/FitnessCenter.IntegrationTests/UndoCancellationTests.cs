using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Members.Application;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.Modules.Members.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// The cancellation confirmation email, and a member undoing a pending cancellation over HTTP
/// (D-055, FIT-95). Emails land in the test mailbox (D-038).
/// </summary>
[TestClass]
public sealed class UndoCancellationTests
{
    // Wednesday 10 March 2027, 10:30 in Copenhagen: D-048's example; effective at the end of 30 April.
    private static readonly Instant Now = Instant.FromUtc(2027, 3, 10, 9, 30);

    private static ApiFactory Factory(IClock clock) => new(services => services.AddSingleton(clock));

    [TestMethod]
    public async Task Cancelling_emails_a_confirmation_with_the_effective_date()
    {
        var email = SyntheticEmail();
        await using var factory = Factory(new MovableClock(Now));
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, email);

        await CancelAsync(client);

        var text = await TestMailbox.TextToAsync(email, MembershipHandler.CancelledSubject);
        StringAssert.Contains(text, "ends after 30 April 2027");
        Assert.AreEqual(1, await TestMailbox.CountToAsync(email, MembershipHandler.CancelledSubject));
    }

    [TestMethod]
    public async Task Undoing_ends_the_pending_cancellation_and_emails_a_confirmation()
    {
        var email = SyntheticEmail();
        var clock = new MovableClock(Now);
        await using var factory = Factory(clock);
        using var client = factory.CreateClient();
        var memberId = await SignUpConfirmedAndLoggedInAsync(client, email);
        Assert.IsTrue((await CancelAsync(client)).CanUndoCancellation);
        clock.Now = Instant.FromUtc(2027, 3, 20, 12, 0);

        using var response = await client.DeleteAsync("/api/members/me/cancellation");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        var membership = await client.GetFromJsonAsync<MyMembership>("/api/members/me");
        Assert.IsNull(membership!.CancelledAt);
        Assert.IsNull(membership.CancellationEffectiveOn);
        Assert.IsFalse(membership.CanUndoCancellation);
        StringAssert.Contains(
            await TestMailbox.TextToAsync(email, MembershipHandler.UndoneSubject), "Your membership continues");

        // The history keeps the undone cancellation (FIT-95).
        var record = (await CancellationsAsync(factory, memberId)).Single();
        Assert.AreEqual(Now, record.CancelledAt);
        Assert.AreEqual(new LocalDate(2027, 4, 30), record.EffectiveOn);
        Assert.AreEqual(clock.Now, record.UndoneAt);
    }

    [TestMethod]
    public async Task Undoing_on_the_effective_date_is_allowed()
    {
        var clock = new MovableClock(Now);
        await using var factory = Factory(clock);
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        await CancelAsync(client);
        // 23:59 on 30 April in Copenhagen (UTC+2).
        clock.Now = Instant.FromUtc(2027, 4, 30, 21, 59);
        Assert.IsTrue((await client.GetFromJsonAsync<MyMembership>("/api/members/me"))!.CanUndoCancellation);

        using var response = await client.DeleteAsync("/api/members/me/cancellation");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.IsNull((await client.GetFromJsonAsync<MyMembership>("/api/members/me"))!.CancelledAt);
    }

    [TestMethod]
    public async Task Undoing_after_the_effective_date_is_refused()
    {
        var email = SyntheticEmail();
        var clock = new MovableClock(Now);
        await using var factory = Factory(clock);
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, email);
        await CancelAsync(client);
        // Midnight into 1 May in Copenhagen: the membership has ended.
        clock.Now = Instant.FromUtc(2027, 4, 30, 22, 0);

        using var response = await client.DeleteAsync("/api/members/me/cancellation");

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        var membership = await client.GetFromJsonAsync<MyMembership>("/api/members/me");
        Assert.AreEqual(Now.ToDateTimeOffset(), membership!.CancelledAt);
        Assert.AreEqual(new DateOnly(2027, 4, 30), membership.CancellationEffectiveOn);
        Assert.IsFalse(membership.CanUndoCancellation);
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, MembershipHandler.UndoneSubject));
    }

    [TestMethod]
    public async Task Undoing_with_nothing_pending_changes_nothing()
    {
        var email = SyntheticEmail();
        await using var factory = Factory(new MovableClock(Now));
        using var client = factory.CreateClient();
        var memberId = await SignUpConfirmedAndLoggedInAsync(client, email);
        var before = await client.GetFromJsonAsync<MyMembership>("/api/members/me");

        using var response = await client.DeleteAsync("/api/members/me/cancellation");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.AreEqual(before, await response.Content.ReadFromJsonAsync<MyMembership>());
        Assert.AreEqual(before, await client.GetFromJsonAsync<MyMembership>("/api/members/me"));
        Assert.IsEmpty(await CancellationsAsync(factory, memberId));
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, MembershipHandler.UndoneSubject));
    }

    /// <summary>A failed send must not undo the cancellation or reverse the undo (D-055).</summary>
    [TestMethod]
    public async Task A_failed_email_keeps_the_cancellation_and_the_undo()
    {
        var email = SyntheticEmail();
        var clock = new MovableClock(Now);
        await using var factory = Factory(clock);
        using (var setup = factory.CreateClient())
        {
            await SignUpConfirmedAndLoggedInAsync(setup, email);
        }

        // Port 1 on the mailbox host: nothing listens there, so sending fails.
        await using var noMail = factory.WithWebHostBuilder(builder => builder.UseSetting("Email:Smtp:Port", "1"));
        using var client = noMail.CreateClient();
        using (var login = await LogInAsync(client, email))
        {
            Assert.IsTrue(login.IsSuccessStatusCode, await login.Content.ReadAsStringAsync());
        }

        var cancelled = await CancelAsync(client);
        Assert.AreEqual(new DateOnly(2027, 4, 30), cancelled.CancellationEffectiveOn);

        using var undo = await client.DeleteAsync("/api/members/me/cancellation");
        Assert.AreEqual(HttpStatusCode.OK, undo.StatusCode, await undo.Content.ReadAsStringAsync());
        Assert.IsNull((await client.GetFromJsonAsync<MyMembership>("/api/members/me"))!.CancelledAt);
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, MembershipHandler.CancelledSubject));
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, MembershipHandler.UndoneSubject));
    }

    [TestMethod]
    public async Task Undoing_needs_a_login()
    {
        await using var factory = Factory(new MovableClock(Now));
        using var client = factory.CreateClient();

        using var response = await client.DeleteAsync("/api/members/me/cancellation");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<MyMembership> CancelAsync(HttpClient client)
    {
        using var response = await client.PostAsync("/api/members/me/cancellation", null);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<MyMembership>())!;
    }

    private static async Task<List<CancellationRecord>> CancellationsAsync(WebApplicationFactory<Program> factory, Guid memberId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MembersDbContext>();
        var member = await db.Members.AsNoTracking().SingleAsync(m => m.Id == new MemberId(memberId));
        return [.. member.Cancellations];
    }

    private sealed record MyMembership(
        string MembershipType,
        long MonthlyPriceOre,
        string Status,
        DateOnly MemberSince,
        DateOnly? PaidUntil,
        DateTimeOffset? CancelledAt,
        DateOnly? CancellationEffectiveOn,
        bool CanUndoCancellation);

    private sealed class MovableClock(Instant now) : IClock
    {
        public Instant Now { get; set; } = now;

        public Instant GetCurrentInstant() => Now;
    }
}
