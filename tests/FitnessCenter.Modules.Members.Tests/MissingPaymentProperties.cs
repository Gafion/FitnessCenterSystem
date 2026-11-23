using CsCheck;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Tests;

/// <summary>
/// Property tests (D-036) for the missing-payment rule (D-020, D-042). The generators aim at
/// the rule's edge, the first Copenhagen midnight after paid-until, and at the days the clocks
/// change, where a UTC date or a 24-hour day would be wrong.
/// </summary>
[TestClass]
public sealed class MissingPaymentProperties
{
    private static readonly LocalDate Epoch = new(2024, 1, 1);

    private static readonly Gen<LocalDate> AnyDay = Gen.Int[0, 3650].Select(days => Epoch.PlusDays(days));

    /// <summary>The last Sunday of March and of October, 2024–2033: the days Denmark changes the clocks.</summary>
    private static readonly LocalDate[] ClockChangeDays = Enumerable.Range(2024, 10)
        .SelectMany(year => new[] { LastSunday(year, 3), LastSunday(year, 10) })
        .ToArray();

    /// <summary>Paid-until dates: any day, or a day at most one away from a clock change.</summary>
    private static readonly Gen<LocalDate> PaidUntil = Gen.OneOf(
        AnyDay,
        Gen.Select(Gen.Int[0, 19].Select(i => ClockChangeDays[i]), Gen.Int[-1, 1]).Select((day, shift) => day.PlusDays(shift)));

    /// <summary>A day offset from paid-until: mostly next to it, sometimes far away.</summary>
    private static readonly Gen<int> DayOffset = Gen.OneOf(Gen.Int[-2, 2], Gen.Int[-400, 400]);

    /// <summary>
    /// Seconds from the first missing midnight: ±1 second, ±10 minutes, ±3 hours (the clock
    /// change happens at 02:00/03:00), or up to two days either way.
    /// </summary>
    private static readonly Gen<long> SecondsFromEdge = Gen.OneOf(
        Gen.Long[-1, 1],
        Gen.Long[-600, 600],
        Gen.Long[-3 * 3600, 3 * 3600],
        Gen.Long[-2 * 86_400, 2 * 86_400]);

    [TestMethod]
    public void The_payment_is_missing_exactly_on_the_days_after_paid_until()
    {
        Gen.Select(PaidUntil, DayOffset).Sample((paidUntil, offset) =>
        {
            var day = paidUntil.PlusDays(offset);
            var expected = day > paidUntil ? PaymentStanding.Missing : PaymentStanding.Paid;
            return MissingPayment.StandingOn(paidUntil, day) == expected;
        });
    }

    /// <summary>The bound the reminder job queries with agrees with the rule, so it finds exactly the missing payments.</summary>
    [TestMethod]
    public void The_query_bound_matches_the_rule()
    {
        Gen.Select(PaidUntil, DayOffset).Sample((paidUntil, offset) =>
        {
            var day = paidUntil.PlusDays(offset);
            var missing = MissingPayment.StandingOn(paidUntil, day) == PaymentStanding.Missing;
            return missing == (paidUntil <= MissingPayment.LatestPaidUntilMissingOn(day));
        });
    }

    /// <summary>Once missing, a payment stays missing on every later day until a new payment moves paid-until.</summary>
    [TestMethod]
    public void A_missing_payment_stays_missing()
    {
        Gen.Select(PaidUntil, DayOffset, Gen.Int[0, 400]).Sample((paidUntil, offset, later) =>
        {
            var day = paidUntil.PlusDays(offset);
            return MissingPayment.StandingOn(paidUntil, day) != PaymentStanding.Missing
                || MissingPayment.StandingOn(paidUntil, day.PlusDays(later)) == PaymentStanding.Missing;
        });
    }

    /// <summary>
    /// For any instant, the payment is missing exactly from the start of the first missing day in
    /// Copenhagen. The oracle asks NodaTime for that midnight, so 23- and 25-hour days count right.
    /// </summary>
    [TestMethod]
    public void The_payment_goes_missing_at_the_first_Copenhagen_midnight_after_paid_until()
    {
        Gen.Select(PaidUntil, SecondsFromEdge).Sample((paidUntil, seconds) =>
        {
            var edge = CenterTime.Zone.AtStartOfDay(paidUntil.PlusDays(1)).ToInstant();
            var now = edge.Plus(Duration.FromSeconds(seconds));
            var expected = now >= edge ? PaymentStanding.Missing : PaymentStanding.Paid;
            return MissingPayment.StandingAt(paidUntil, now, CenterTime.Zone) == expected;
        });
    }

    [TestMethod]
    public void A_member_who_never_paid_is_never_missing_a_payment()
    {
        AnyDay.Sample(day => MissingPayment.StandingOn(null, day) == PaymentStanding.NeverPaid);
    }

    private static LocalDate LastSunday(int year, int month)
    {
        var day = new LocalDate(year, month, 1).PlusMonths(1).PlusDays(-1);
        while (day.DayOfWeek != IsoDayOfWeek.Sunday)
        {
            day = day.PlusDays(-1);
        }

        return day;
    }
}
