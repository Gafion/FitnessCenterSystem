using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Tests;

/// <summary>
/// The missing-payment rule (D-020, D-042): no grace period, so the payment is missing from the
/// first Europe/Copenhagen calendar day after paid-until.
/// </summary>
[TestClass]
public sealed class MissingPaymentTests
{
    private static LocalDate Date(int year, int month, int day) => new(year, month, day);

    [TestMethod]
    public void A_member_who_never_paid_has_no_missing_payment_but_is_never_paid()
    {
        Assert.AreEqual(PaymentStanding.NeverPaid, MissingPayment.StandingOn(null, Date(2027, 1, 12)));
    }

    [TestMethod]
    [DataRow(2027, 1, 1, "Paid", DisplayName = "well before paid-until")]
    [DataRow(2027, 1, 31, "Paid", DisplayName = "on the paid-until date itself")]
    [DataRow(2027, 2, 1, "Missing", DisplayName = "the first day after paid-until: no grace period")]
    [DataRow(2027, 6, 1, "Missing", DisplayName = "months later")]
    public void The_payment_is_missing_from_the_day_after_paid_until(int year, int month, int day, string expected)
    {
        Assert.AreEqual(expected, MissingPayment.StandingOn(Date(2027, 1, 31), Date(year, month, day)).ToString());
    }

    [TestMethod]
    public void The_first_missing_day_is_the_day_after_paid_until()
    {
        Assert.AreEqual(Date(2027, 3, 1), MissingPayment.FirstMissingDay(Date(2027, 2, 28)));
        Assert.AreEqual(Date(2028, 2, 29), MissingPayment.FirstMissingDay(Date(2028, 2, 28)));
        Assert.AreEqual(Date(2027, 1, 1), MissingPayment.FirstMissingDay(Date(2026, 12, 31)));
    }

    /// <summary>
    /// The day turns at midnight in Copenhagen, not in UTC, also on the two days a year when the
    /// clocks change. Each row gives the last second covered and the first second missing.
    /// </summary>
    [TestMethod]
    [DataRow("2027-01-31", "2027-01-31T22:59:59Z", "2027-01-31T23:00:00Z", DisplayName = "winter time (UTC+1)")]
    [DataRow("2027-07-31", "2027-07-31T21:59:59Z", "2027-07-31T22:00:00Z", DisplayName = "summer time (UTC+2)")]
    [DataRow("2027-03-27", "2027-03-27T22:59:59Z", "2027-03-27T23:00:00Z", DisplayName = "paid until the eve of the spring change")]
    [DataRow("2027-03-28", "2027-03-28T21:59:59Z", "2027-03-28T22:00:00Z", DisplayName = "paid until the 23-hour spring day")]
    [DataRow("2027-10-30", "2027-10-30T21:59:59Z", "2027-10-30T22:00:00Z", DisplayName = "paid until the eve of the autumn change")]
    [DataRow("2027-10-31", "2027-10-31T22:59:59Z", "2027-10-31T23:00:00Z", DisplayName = "paid until the 25-hour autumn day")]
    public void The_day_turns_at_midnight_in_Copenhagen(string paidUntil, string lastPaidSecond, string firstMissingSecond)
    {
        var until = NodaTime.Text.LocalDatePattern.Iso.Parse(paidUntil).Value;

        Assert.AreEqual(PaymentStanding.Paid, MissingPayment.StandingAt(until, At(lastPaidSecond), CenterTime.Zone));
        Assert.AreEqual(PaymentStanding.Missing, MissingPayment.StandingAt(until, At(firstMissingSecond), CenterTime.Zone));
    }

    [TestMethod]
    public void A_member_paid_for_a_past_month_has_a_missing_payment()
    {
        var member = Member.SignUp(
            Guid.Parse("0199a3c0-0000-7000-8000-00000000abcd"),
            MembershipType.General,
            new SignupConsents(TermsAccepted: true, "terms-v-test", EarlyStartRequested: false, "early-v-test"),
            Instant.FromUtc(2027, 1, 12, 12, 0),
            CenterTime.Zone);
        var now = Instant.FromUtc(2027, 1, 12, 12, 0);
        Assert.AreEqual(PaymentStanding.NeverPaid, member.PaymentStandingAt(now, CenterTime.Zone));

        member.ApplyPayment(PaymentPeriod.MonthFrom(Date(2026, 11, 1)));

        Assert.AreEqual(PaymentStanding.Missing, member.PaymentStandingAt(now, CenterTime.Zone));
        member.ApplyPayment(PaymentPeriod.MonthFrom(Date(2026, 12, 1)));
        Assert.AreEqual(PaymentStanding.Missing, member.PaymentStandingAt(now, CenterTime.Zone));
        member.ApplyPayment(PaymentPeriod.MonthFrom(Date(2027, 1, 1)));
        Assert.AreEqual(PaymentStanding.Paid, member.PaymentStandingAt(now, CenterTime.Zone));
    }

    private static Instant At(string text) => NodaTime.Text.InstantPattern.General.Parse(text).Value;
}
