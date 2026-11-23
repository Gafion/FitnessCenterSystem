using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Tests;

/// <summary>Payment periods and the paid-until date (D-020).</summary>
[TestClass]
public sealed class MemberPaymentTests
{
    private static readonly Instant SignedUpAt = Instant.FromUtc(2026, 11, 10, 12, 0);

    private static Member NewMember() => Member.SignUp(
        Guid.Parse("0199a3c0-0000-7000-8000-00000000abcd"),
        MembershipType.General,
        new SignupConsents(TermsAccepted: true, "terms-v-test", EarlyStartRequested: false, "early-v-test"),
        SignedUpAt,
        CenterTime.Zone);

    private static LocalDate Date(int year, int month, int day) => new(year, month, day);

    [TestMethod]
    [DataRow(2026, 3, 1, "2026-03-31", DisplayName = "a whole calendar month")]
    [DataRow(2026, 3, 15, "2026-04-14", DisplayName = "mid-month to mid-month")]
    [DataRow(2026, 12, 20, "2027-01-19", DisplayName = "across the new year")]
    [DataRow(2028, 2, 1, "2028-02-29", DisplayName = "February in a leap year")]
    [DataRow(2027, 1, 31, "2027-02-27", DisplayName = "the 31st: February has no 31st")]
    public void A_monthly_period_ends_the_day_before_the_same_day_next_month(int year, int month, int day, string expected)
    {
        var period = PaymentPeriod.MonthFrom(Date(year, month, day));

        Assert.AreEqual(Date(year, month, day), period.From);
        Assert.AreEqual(expected, period.To.ToString("uuuu-MM-dd", null));
    }

    [TestMethod]
    public void A_period_cannot_end_before_it_starts()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new PaymentPeriod(Date(2026, 3, 2), Date(2026, 3, 1)));
    }

    [TestMethod]
    public void The_first_payment_activates_the_membership_and_sets_paid_until()
    {
        var member = NewMember();

        var outcome = member.ApplyPayment(PaymentPeriod.MonthFrom(Date(2026, 11, 10)));

        Assert.AreEqual(PaymentOutcome.Applied, outcome);
        Assert.AreEqual(MemberStatus.Active, member.Status);
        Assert.AreEqual(Date(2026, 12, 9), member.PaidUntil);
    }

    [TestMethod]
    public void The_first_payment_may_cover_a_past_period()
    {
        var member = NewMember();

        member.ApplyPayment(PaymentPeriod.MonthFrom(Date(2026, 9, 1)));

        Assert.AreEqual(Date(2026, 9, 30), member.PaidUntil);
    }

    [TestMethod]
    public void A_payment_that_follows_on_moves_paid_until()
    {
        var member = NewMember();
        member.ApplyPayment(PaymentPeriod.MonthFrom(Date(2026, 11, 1)));

        var outcome = member.ApplyPayment(PaymentPeriod.MonthFrom(Date(2026, 12, 1)));

        Assert.AreEqual(PaymentOutcome.Applied, outcome);
        Assert.AreEqual(Date(2026, 12, 31), member.PaidUntil);
    }

    [TestMethod]
    [DataRow(2026, 12, 2, DisplayName = "a gap of one day")]
    [DataRow(2026, 11, 30, DisplayName = "an overlap of one day")]
    [DataRow(2026, 11, 1, DisplayName = "the same period again")]
    [DataRow(2026, 10, 1, DisplayName = "an earlier period")]
    public void A_payment_with_a_gap_or_overlap_changes_nothing(int year, int month, int day)
    {
        var member = NewMember();
        member.ApplyPayment(PaymentPeriod.MonthFrom(Date(2026, 11, 1)));

        var outcome = member.ApplyPayment(PaymentPeriod.MonthFrom(Date(year, month, day)));

        Assert.AreEqual(PaymentOutcome.NotContiguous, outcome);
        Assert.AreEqual(Date(2026, 11, 30), member.PaidUntil);
    }

    [TestMethod]
    public void The_next_period_starts_today_before_the_first_payment_then_after_paid_until()
    {
        var member = NewMember();
        var today = Date(2026, 11, 23);
        Assert.AreEqual(today, member.NextPeriodStart(today));

        member.ApplyPayment(PaymentPeriod.MonthFrom(Date(2026, 9, 1)));

        // A member who skipped payments catches up from the day after paid-until, not today.
        Assert.AreEqual(Date(2026, 10, 1), member.NextPeriodStart(today));
    }

    [TestMethod]
    [DataRow("1234", DisplayName = "four digits")]
    public void A_payment_record_keeps_the_last_four_digits(string last4)
    {
        var record = Record(last4);

        Assert.AreEqual(last4, record.CardLast4);
    }

    [TestMethod]
    [DataRow("4242424242424242", DisplayName = "a full card number")]
    [DataRow("123", DisplayName = "three digits")]
    [DataRow("12a4", DisplayName = "not digits")]
    [DataRow("", DisplayName = "empty")]
    public void A_payment_record_refuses_anything_but_four_digits(string cardDigits)
    {
        Assert.ThrowsExactly<ArgumentException>(() => Record(cardDigits));
    }

    private static PaymentRecord Record(string cardLast4) => new(
        PaymentRecordId.New(),
        MemberId.New(),
        "test-ref",
        Money.FromKroner(299),
        SignedUpAt,
        cardLast4,
        PaymentPeriod.MonthFrom(Date(2026, 11, 10)));
}
