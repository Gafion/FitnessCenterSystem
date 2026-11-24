using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>A member cancels their membership from the account page over HTTP (D-048, FIT-70).</summary>
[TestClass]
public sealed class CancellationTests
{
    // Wednesday 10 March 2027, 22:30 in Copenhagen (UTC+1): D-048's example date, late evening.
    private static readonly Instant Now = Instant.FromUtc(2027, 3, 10, 21, 30);

    private static ApiFactory Factory(IClock clock) => new(services => services.AddSingleton(clock));

    [TestMethod]
    public async Task Cancelling_stores_the_instant_and_the_effective_date()
    {
        await using var factory = Factory(new MovableClock(Now));
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        using var response = await client.PostAsync("/api/members/me/cancellation", null);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        var cancelled = await response.Content.ReadFromJsonAsync<MyMembership>();
        Assert.AreEqual(new DateOnly(2027, 4, 30), cancelled!.CancellationEffectiveOn);
        Assert.AreEqual(Now.ToDateTimeOffset(), cancelled.CancelledAt);

        var membership = await client.GetFromJsonAsync<MyMembership>("/api/members/me");
        Assert.AreEqual(cancelled, membership);
    }

    [TestMethod]
    public async Task Cancelling_twice_keeps_the_first_cancellation()
    {
        var clock = new MovableClock(Now);
        await using var factory = Factory(clock);
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        using (var first = await client.PostAsync("/api/members/me/cancellation", null))
        {
            Assert.AreEqual(HttpStatusCode.OK, first.StatusCode, await first.Content.ReadAsStringAsync());
        }

        // A month later the second cancellation would move the effective date; it must not.
        clock.Now = Instant.FromUtc(2027, 4, 12, 10, 0);
        using var second = await client.PostAsync("/api/members/me/cancellation", null);

        Assert.AreEqual(HttpStatusCode.Conflict, second.StatusCode);
        var membership = await client.GetFromJsonAsync<MyMembership>("/api/members/me");
        Assert.AreEqual(Now.ToDateTimeOffset(), membership!.CancelledAt);
        Assert.AreEqual(new DateOnly(2027, 4, 30), membership.CancellationEffectiveOn);
    }

    [TestMethod]
    public async Task A_member_who_has_not_cancelled_has_no_cancellation()
    {
        await using var factory = Factory(new MovableClock(Now));
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        var membership = await client.GetFromJsonAsync<MyMembership>("/api/members/me");

        Assert.IsNull(membership!.CancelledAt);
        Assert.IsNull(membership.CancellationEffectiveOn);
    }

    [TestMethod]
    public async Task Cancelling_needs_a_login()
    {
        await using var factory = Factory(new MovableClock(Now));
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/members/me/cancellation", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record MyMembership(
        string MembershipType,
        long MonthlyPriceOre,
        string Status,
        DateOnly MemberSince,
        DateOnly? PaidUntil,
        DateTimeOffset? CancelledAt,
        DateOnly? CancellationEffectiveOn);

    private sealed class MovableClock(Instant now) : IClock
    {
        public Instant Now { get; set; } = now;

        public Instant GetCurrentInstant() => Now;
    }
}
