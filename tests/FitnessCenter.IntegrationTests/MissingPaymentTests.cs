using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Access.Infrastructure;
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
/// A missing payment refuses check-in and sends one reminder email (D-020, D-042). Every test
/// fixes the clock: the reminders scan all members in the shared database, so all tests that run
/// them use the same day, <see cref="Now"/>.
/// </summary>
[TestClass]
public sealed class MissingPaymentTests
{
    private const string SwipeMyCardUri = "/api/access/simulation/reader/swipes/my-card";

    // Tuesday 12 January 2027, 07:30 in Copenhagen.
    private static readonly Instant Now = Instant.FromUtc(2027, 1, 12, 6, 30);

    private static ApiFactory Factory(IClock? clock = null, IReadOnlyDictionary<string, string?>? settings = null) =>
        new(services => services.AddSingleton(clock ?? new MutableClock(Now)), settings);

    [TestMethod]
    public async Task A_member_whose_payment_is_missing_is_refused_and_nothing_is_recorded()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var memberId = await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        await PayAsync(client, new DateOnly(2026, 11, 1)); // paid until 30 November 2026

        var result = await SwipeMyCardAsync(client);

        Assert.AreEqual("payment-missing", result.Outcome);
        Assert.IsNull(result.FirstName, "the door screen is public, so a refusal doesn't name the member");
        Assert.AreEqual(0, await AttendancesAsync(factory, memberId));
    }

    [TestMethod]
    public async Task A_member_who_paid_for_today_is_welcomed()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var memberId = await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        await PayAsync(client, periodFrom: null);

        var result = await SwipeMyCardAsync(client);

        Assert.AreEqual("welcome", result.Outcome);
        Assert.AreEqual(1, await AttendancesAsync(factory, memberId));
    }

    /// <summary>
    /// DECISION NEEDED (FIT-45): D-020 and D-042 don't say whether a member who never paid may
    /// check in. Until the owner decides, they are let in as before; this test pins that seam.
    /// </summary>
    [TestMethod]
    public async Task A_member_who_never_paid_is_let_in_until_the_owner_decides()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        var result = await SwipeMyCardAsync(client);

        Assert.AreEqual("welcome", result.Outcome);
    }

    /// <summary>No grace period: the last second of the paid-until day in Copenhagen is fine, the next is not (D-042).</summary>
    [TestMethod]
    public async Task Check_in_is_refused_from_the_first_Copenhagen_midnight_after_paid_until()
    {
        var clock = new MutableClock(Instant.FromUtc(2027, 1, 11, 22, 59, 59)); // 23:59:59 on 11 January in Copenhagen
        await using var factory = Factory(clock);
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        await PayAsync(client, new DateOnly(2026, 12, 12)); // paid until 11 January 2027

        var lastSecond = await SwipeMyCardAsync(client);
        clock.Now = Instant.FromUtc(2027, 1, 11, 23, 0); // midnight in Copenhagen, still 11 January in UTC
        var nextSecond = await SwipeMyCardAsync(client);

        Assert.AreEqual("welcome", lastSecond.Outcome);
        Assert.AreEqual("payment-missing", nextSecond.Outcome);
    }

    [TestMethod]
    public async Task A_missing_payment_sends_one_reminder_even_across_restarts()
    {
        var email = SyntheticEmail();
        Guid memberId;
        await using (var factory = Factory())
        {
            using var client = factory.CreateClient();
            memberId = await SignUpConfirmedAndLoggedInAsync(client, email);
            await PayAsync(client, new DateOnly(2026, 11, 1));

            await SendDueRemindersAsync(factory);
            await SendDueRemindersAsync(factory);
        }

        // A new API on the same database, as after a restart.
        await using (var restarted = Factory())
        {
            await SendDueRemindersAsync(restarted);

            var text = await TestMailbox.TextToAsync(email, PaymentReminders.Subject);
            StringAssert.Contains(text, "Hi Test,");
            StringAssert.Contains(text, "after 30 November 2026");
            Assert.AreEqual(1, await TestMailbox.CountToAsync(email, PaymentReminders.Subject));
            Assert.IsNotNull((await RemindersAsync(restarted, memberId)).Single().SentAt);
        }
    }

    /// <summary>Several reminder runs at once, as with two API instances: the claim lets only one send.</summary>
    [TestMethod]
    public async Task Simultaneous_reminder_runs_send_one_email()
    {
        var email = SyntheticEmail();
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var memberId = await SignUpConfirmedAndLoggedInAsync(client, email);
        await PayAsync(client, new DateOnly(2026, 11, 1));

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => SendDueRemindersAsync(factory)));

        await TestMailbox.TextToAsync(email, PaymentReminders.Subject);
        Assert.AreEqual(1, await TestMailbox.CountToAsync(email, PaymentReminders.Subject));
        Assert.HasCount(1, await RemindersAsync(factory, memberId));
    }

    [TestMethod]
    public async Task No_reminder_while_today_is_paid_for()
    {
        var email = SyntheticEmail();
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var memberId = await SignUpConfirmedAndLoggedInAsync(client, email);
        await PayAsync(client, new DateOnly(2026, 12, 13)); // paid until 12 January 2027, which is today

        await SendDueRemindersAsync(factory);

        Assert.IsEmpty(await RemindersAsync(factory, memberId));
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, PaymentReminders.Subject));
    }

    /// <summary>
    /// If the email can't be sent, nothing is stored as sent, and the next run sends it. Not run
    /// alongside other tests: its failing run would also claim and release their members' reminders.
    /// </summary>
    [TestMethod]
    [DoNotParallelize]
    public async Task A_reminder_that_could_not_be_sent_is_sent_on_the_next_run()
    {
        var email = SyntheticEmail();
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var memberId = await SignUpConfirmedAndLoggedInAsync(client, email);
        await PayAsync(client, new DateOnly(2026, 11, 1));

        // Port 1 on the mailbox host: nothing listens there, so sending fails.
        await using (var noMail = factory.WithWebHostBuilder(builder => builder.UseSetting("Email:Smtp:Port", "1")))
        {
            await SendDueRemindersAsync(noMail);
            Assert.IsEmpty(await RemindersAsync(noMail, memberId));
        }

        await SendDueRemindersAsync(factory);

        await TestMailbox.TextToAsync(email, PaymentReminders.Subject);
        Assert.AreEqual(1, await TestMailbox.CountToAsync(email, PaymentReminders.Subject));
    }

    /// <summary>
    /// The hosted job runs the reminders on its own, with the injected clock. Not run alongside
    /// other tests: the job would also send their members' reminders while they check them.
    /// </summary>
    [TestMethod]
    [DoNotParallelize]
    public async Task The_reminder_job_sends_the_reminder_by_itself()
    {
        var email = SyntheticEmail();
        await using var factory = Factory(settings: new Dictionary<string, string?>
        {
            ["Members:PaymentReminders:Enabled"] = "true",
            ["Members:PaymentReminders:Interval"] = "00:00:01",
        });
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, email);
        await PayAsync(client, new DateOnly(2026, 11, 1));

        StringAssert.Contains(await TestMailbox.TextToAsync(email, PaymentReminders.Subject), "after 30 November 2026");
    }

    private static async Task PayAsync(HttpClient client, DateOnly? periodFrom)
    {
        using var response = await client.PostAsJsonAsync("/api/simulation/payments", new { periodFrom });
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task<SwipeResult> SwipeMyCardAsync(HttpClient client)
    {
        using var swipe = await client.PostAsync(SwipeMyCardUri, null);
        Assert.AreEqual(HttpStatusCode.OK, swipe.StatusCode, await swipe.Content.ReadAsStringAsync());
        return (await swipe.Content.ReadFromJsonAsync<SwipeResult>())!;
    }

    private static async Task SendDueRemindersAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<PaymentReminders>().SendDueAsync(CancellationToken.None);
    }

    private static async Task<List<PaymentReminder>> RemindersAsync(WebApplicationFactory<Program> factory, Guid memberId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MembersDbContext>();
        return await db.PaymentReminders.Where(r => r.MemberId == new MemberId(memberId)).ToListAsync();
    }

    private static async Task<int> AttendancesAsync(ApiFactory factory, Guid memberId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        return await db.Attendances.CountAsync(a => a.MemberId == memberId);
    }

    private sealed class MutableClock(Instant now) : IClock
    {
        public Instant Now { get; set; } = now;

        public Instant GetCurrentInstant() => Now;
    }

    private sealed record SwipeResult(string Outcome, string? FirstName, DateTimeOffset At);
}
