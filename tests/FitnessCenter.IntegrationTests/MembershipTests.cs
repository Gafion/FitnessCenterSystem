using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>The account page's membership section over HTTP (D-020, D-022).</summary>
[TestClass]
public sealed class MembershipTests
{
    // Monday 23 November 2026, 13:00 in Copenhagen.
    private static readonly Instant Now = Instant.FromUtc(2026, 11, 23, 12, 0);

    private static ApiFactory Factory() => new(
        services => services.AddSingleton<IClock>(new FixedClock(Now)),
        new Dictionary<string, string?> { ["Simulation:Enabled"] = "true" });

    [TestMethod]
    public async Task A_new_member_sees_the_membership_awaiting_the_first_payment()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        var membership = await client.GetFromJsonAsync<MyMembership>("/api/members/me");

        Assert.AreEqual(
            new MyMembership("General membership", 29_900, "AwaitingFirstPayment", new DateOnly(2026, 11, 23), null),
            membership);
    }

    [TestMethod]
    public async Task A_payment_makes_the_membership_active_with_a_paid_until_date()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        using (var pay = await client.PostAsJsonAsync("/api/simulation/payments", new { }))
        {
            Assert.AreEqual(HttpStatusCode.Created, pay.StatusCode, await pay.Content.ReadAsStringAsync());
        }

        var membership = await client.GetFromJsonAsync<MyMembership>("/api/members/me");

        Assert.AreEqual("Active", membership!.Status);
        Assert.AreEqual(new DateOnly(2026, 12, 22), membership.PaidUntil);
        Assert.AreEqual(new DateOnly(2026, 11, 23), membership.MemberSince);
    }

    [TestMethod]
    public async Task The_membership_needs_a_login()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/members/me");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record MyMembership(
        string MembershipType, long MonthlyPriceOre, string Status, DateOnly MemberSince, DateOnly? PaidUntil);

    private sealed class FixedClock(Instant now) : IClock
    {
        public Instant GetCurrentInstant() => now;
    }
}
