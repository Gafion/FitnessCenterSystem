using CsCheck;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Tests;

/// <summary>
/// Property tests (D-036): the paid-until rules (D-020) hold for any start date and any
/// sequence of payments, not only the examples in <see cref="MemberPaymentTests"/>.
/// </summary>
[TestClass]
public sealed class MemberPaymentProperties
{
    private static readonly LocalDate Epoch = new(2024, 1, 1);

    /// <summary>Any day from 2024 to about 2033, including month ends and leap days.</summary>
    private static readonly Gen<LocalDate> AnyDay = Gen.Int[0, 3650].Select(days => Epoch.PlusDays(days));

    [TestMethod]
    public void A_monthly_period_is_28_to_31_days_and_the_next_one_follows_on()
    {
        AnyDay.Sample(from =>
        {
            var period = PaymentPeriod.MonthFrom(from);
            var days = Period.DaysBetween(period.From, period.To) + 1;
            var next = PaymentPeriod.MonthFrom(period.To.PlusDays(1));
            return days is >= 28 and <= 31 && next.From == period.To.PlusDays(1);
        });
    }

    /// <summary>Paying the "next month" again and again: paid-until is always the end of the last period.</summary>
    [TestMethod]
    public void Paying_next_month_repeatedly_always_follows_on()
    {
        Gen.Select(AnyDay, Gen.Int[1, 30]).Sample((today, payments) =>
        {
            var member = NewMember();
            var paidUntils = new List<LocalDate>();
            for (var i = 0; i < payments; i++)
            {
                var period = PaymentPeriod.MonthFrom(member.NextPeriodStart(today));
                if (member.ApplyPayment(period) != PaymentOutcome.Applied || member.PaidUntil != period.To)
                {
                    return false;
                }

                paidUntils.Add(period.To);
            }

            return member.Status == MemberStatus.Active
                && paidUntils.Zip(paidUntils.Skip(1)).All(pair => pair.First < pair.Second);
        });
    }

    /// <summary>Any mix of payments: paid-until only ever moves forward, and a refused one changes nothing.</summary>
    [TestMethod]
    public void Paid_until_never_moves_back_and_refused_payments_change_nothing()
    {
        // Each step pays either the next month or a random one, so both outcomes come up often.
        var step = Gen.Select(Gen.Bool, AnyDay);
        Gen.Select(AnyDay, step.Array[1, 20]).Sample((first, steps) =>
        {
            var member = NewMember();
            member.ApplyPayment(PaymentPeriod.MonthFrom(first));
            foreach (var (payNext, randomStart) in steps)
            {
                var before = member.PaidUntil!.Value;
                var start = payNext ? before.PlusDays(1) : randomStart;
                var outcome = member.ApplyPayment(PaymentPeriod.MonthFrom(start));
                var expected = start == before.PlusDays(1) ? PaymentOutcome.Applied : PaymentOutcome.NotContiguous;
                if (outcome != expected || member.PaidUntil < before || (outcome == PaymentOutcome.NotContiguous && member.PaidUntil != before))
                {
                    return false;
                }
            }

            return true;
        });
    }

    private static Member NewMember() => Member.SignUp(
        Guid.Parse("0199a3c0-0000-7000-8000-00000000abcd"),
        MembershipType.General,
        new SignupConsents(TermsAccepted: true, "terms-v-test", EarlyStartRequested: false, "early-v-test"),
        Instant.FromUtc(2024, 1, 1, 12, 0),
        CenterTime.Zone);
}
