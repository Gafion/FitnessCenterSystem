using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Members.Contracts;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>Payment history and simulated payments over HTTP (D-020, D-021, D-041).</summary>
[TestClass]
public sealed class PaymentTests
{
    // Monday 23 November 2026, 13:00 in Copenhagen.
    private static readonly Instant Now = Instant.FromUtc(2026, 11, 23, 12, 0);

    private static ApiFactory Factory(bool simulation = true) => new(
        services => services.AddSingleton<IClock>(new FixedClock(Now)),
        new Dictionary<string, string?> { ["Simulation:Enabled"] = simulation ? "true" : "false" });

    [TestMethod]
    public async Task The_frontend_can_ask_whether_the_simulation_is_on()
    {
        await using var on = Factory(simulation: true);
        await using var off = Factory(simulation: false);

        Assert.IsTrue((await on.CreateClient().GetFromJsonAsync<Settings>("/api/simulation"))!.Enabled);
        Assert.IsFalse((await off.CreateClient().GetFromJsonAsync<Settings>("/api/simulation"))!.Enabled);
    }

    [TestMethod]
    public async Task Development_turns_the_simulation_on()
    {
        await using var factory = new ApiFactory();

        Assert.IsTrue((await factory.CreateClient().GetFromJsonAsync<Settings>("/api/simulation"))!.Enabled);
    }

    [TestMethod]
    public async Task A_new_member_has_no_payments_yet()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        var payments = await GetPaymentsAsync(client);

        Assert.AreEqual("AwaitingFirstPayment", payments.Status);
        Assert.IsNull(payments.PaidUntil);
        Assert.AreEqual(29_900, payments.MonthlyPriceOre);
        Assert.IsEmpty(payments.Payments);
    }

    [TestMethod]
    public async Task Paying_next_month_twice_activates_the_member_and_extends_paid_until()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        var first = await PayAsync(client, periodFrom: null);
        var second = await PayAsync(client, periodFrom: null);

        Assert.AreEqual(new Payment(29_900, Now.ToDateTimeOffset(), "4242", new(2026, 11, 23), new(2026, 12, 22)), first);
        Assert.AreEqual(new DateOnly(2026, 12, 23), second.PeriodFrom);
        Assert.AreEqual(new DateOnly(2027, 1, 22), second.PeriodTo);
        var payments = await GetPaymentsAsync(client);
        Assert.AreEqual("Active", payments.Status);
        Assert.AreEqual(new DateOnly(2027, 1, 22), payments.PaidUntil);
        CollectionAssert.AreEqual(new[] { second, first }, payments.Payments, "newest period first");
    }

    /// <summary>A first payment for a past month leaves paid-until in the past: a missing payment (FIT-45).</summary>
    [TestMethod]
    public async Task A_payment_for_a_past_period_leaves_paid_until_in_the_past()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        await PayAsync(client, periodFrom: new DateOnly(2026, 9, 1));
        Assert.AreEqual(new DateOnly(2026, 9, 30), (await GetPaymentsAsync(client)).PaidUntil);

        // "Pay next month" then catches up from the day after paid-until.
        var next = await PayAsync(client, periodFrom: null);
        Assert.AreEqual(new DateOnly(2026, 10, 1), next.PeriodFrom);
        Assert.AreEqual(new DateOnly(2026, 10, 31), (await GetPaymentsAsync(client)).PaidUntil);
    }

    [TestMethod]
    public async Task A_payment_with_a_gap_is_refused_and_not_recorded()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        await PayAsync(client, periodFrom: new DateOnly(2026, 9, 1));

        using var gap = await client.PostAsJsonAsync("/api/simulation/payments", new { periodFrom = "2026-11-01" });

        Assert.AreEqual(HttpStatusCode.Conflict, gap.StatusCode);
        Assert.Contains("period-not-contiguous", await gap.Content.ReadAsStringAsync());
        var payments = await GetPaymentsAsync(client);
        Assert.HasCount(1, payments.Payments);
        Assert.AreEqual(new DateOnly(2026, 9, 30), payments.PaidUntil);
    }

    [TestMethod]
    public async Task The_simulation_answers_404_when_it_is_off()
    {
        await using var factory = Factory(simulation: false);
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        using var pay = await client.PostAsJsonAsync("/api/simulation/payments", new { });

        Assert.AreEqual(HttpStatusCode.NotFound, pay.StatusCode);
        Assert.IsEmpty((await GetPaymentsAsync(client)).Payments, "the history still works, and nothing was paid");
    }

    /// <summary>D-041 checks the setting first: off means 404, with or without a login.</summary>
    [TestMethod]
    public async Task The_simulation_answers_404_when_it_is_off_even_without_a_login()
    {
        await using var factory = Factory(simulation: false);
        using var client = factory.CreateClient();

        using var pay = await client.PostAsJsonAsync("/api/simulation/payments", new { });

        Assert.AreEqual(HttpStatusCode.NotFound, pay.StatusCode);
    }

    [TestMethod]
    public async Task Payments_need_a_login()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();

        using var history = await client.GetAsync("/api/members/me/payments");
        using var pay = await client.PostAsJsonAsync("/api/simulation/payments", new { });

        Assert.AreEqual(HttpStatusCode.Unauthorized, history.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, pay.StatusCode);
    }

    /// <summary>The Payments service will deliver at least once (architecture.md §13): a repeat is recorded once.</summary>
    [TestMethod]
    public async Task The_same_confirmation_twice_is_recorded_once()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var memberId = await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        await PayAsync(client, periodFrom: null);
        var confirmation = new PaymentConfirmed(
            $"test-{Guid.NewGuid()}", memberId, 29_900, Now, "1234", new LocalDate(2026, 12, 23), new LocalDate(2027, 1, 22));

        var first = await ReceiveAsync(factory, confirmation);
        var repeat = await ReceiveAsync(factory, confirmation);

        Assert.AreEqual(PaymentConfirmationResult.Recorded, first);
        Assert.AreEqual(PaymentConfirmationResult.AlreadyRecorded, repeat);
        var payments = await GetPaymentsAsync(client);
        Assert.HasCount(2, payments.Payments);
        Assert.AreEqual("1234", payments.Payments[0].CardLast4);
        Assert.AreEqual(new DateOnly(2027, 1, 22), payments.PaidUntil);
    }

    [TestMethod]
    public async Task A_confirmation_for_an_unknown_member_is_not_recorded()
    {
        await using var factory = Factory();

        var result = await ReceiveAsync(factory, new PaymentConfirmed(
            $"test-{Guid.NewGuid()}", Guid.NewGuid(), 29_900, Now, "1234", new LocalDate(2026, 12, 1), new LocalDate(2026, 12, 31)));

        Assert.AreEqual(PaymentConfirmationResult.UnknownMember, result);
    }

    private static async Task<PaymentConfirmationResult> ReceiveAsync(ApiFactory factory, PaymentConfirmed confirmation)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IPaymentConfirmations>()
            .ReceiveAsync(confirmation, CancellationToken.None);
    }

    private static async Task<Payment> PayAsync(HttpClient client, DateOnly? periodFrom)
    {
        using var response = await client.PostAsJsonAsync("/api/simulation/payments", new { periodFrom });
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<Payment>())!;
    }

    private static async Task<MyPayments> GetPaymentsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<MyPayments>("/api/members/me/payments"))!;

    private sealed record Settings(bool Enabled);

    private sealed record Payment(long AmountOre, DateTimeOffset PaidAt, string CardLast4, DateOnly PeriodFrom, DateOnly PeriodTo);

    private sealed record MyPayments(string Status, DateOnly? PaidUntil, long MonthlyPriceOre, Payment[] Payments);

    private sealed class FixedClock(Instant now) : IClock
    {
        public Instant GetCurrentInstant() => now;
    }
}
