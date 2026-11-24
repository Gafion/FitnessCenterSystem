using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Tests;

/// <summary>One month's notice to the end of a month (D-048), with fixed instants in place of the clock.</summary>
[TestClass]
public sealed class CancellationNoticeTests
{
    private static readonly SignupConsents Consents =
        new(TermsAccepted: true, "terms-v-test", EarlyStartRequested: false, "early-v-test");

    /// <summary>D-048's example: cancelled 10 March, takes effect at the end of 30 April, access ends 1 May.</summary>
    [TestMethod]
    public void The_D048_example_takes_effect_at_the_end_of_30_April()
    {
        var effective = CancellationNotice.EffectiveDate(new LocalDate(2027, 3, 10));

        Assert.AreEqual(new LocalDate(2027, 4, 30), effective);
        Assert.AreEqual(new LocalDate(2027, 5, 1), effective.PlusDays(1));
    }

    [TestMethod]
    [DataRow("2027-03-31", "2027-04-30", DisplayName = "last day of March")]
    [DataRow("2027-01-31", "2027-02-28", DisplayName = "last day of January, not a leap year")]
    [DataRow("2028-01-31", "2028-02-29", DisplayName = "last day of January, leap year")]
    [DataRow("2027-02-28", "2027-03-31", DisplayName = "last day of February")]
    [DataRow("2026-12-31", "2027-01-31", DisplayName = "last day of the year")]
    [DataRow("2027-04-30", "2027-05-31", DisplayName = "last day of a 30-day month")]
    public void Cancelling_on_the_last_day_of_a_month(string cancelledOn, string expected)
    {
        Assert.AreEqual(Date(expected), CancellationNotice.EffectiveDate(Date(cancelledOn)));
    }

    [TestMethod]
    [DataRow("2027-03-01", "2027-04-30", DisplayName = "first day of March")]
    [DataRow("2026-11-24", "2026-12-31", DisplayName = "mid-November")]
    [DataRow("2026-12-01", "2027-01-31", DisplayName = "into the next year")]
    public void Cancelling_during_a_month(string cancelledOn, string expected)
    {
        Assert.AreEqual(Date(expected), CancellationNotice.EffectiveDate(Date(cancelledOn)));
    }

    // Late evening in Copenhagen is still the same day there while UTC may say otherwise, and
    // midnight in Copenhagen is the previous day in UTC. +1 in winter, +2 in summer.
    [TestMethod]
    [DataRow(2027, 1, 31, 22, 59, "2027-02-28", DisplayName = "winter, 23:59 on 31 Jan in Copenhagen")]
    [DataRow(2027, 1, 31, 23, 0, "2027-03-31", DisplayName = "winter, midnight into 1 Feb in Copenhagen")]
    [DataRow(2027, 6, 30, 21, 59, "2027-07-31", DisplayName = "summer, 23:59 on 30 Jun in Copenhagen")]
    [DataRow(2027, 6, 30, 22, 0, "2027-08-31", DisplayName = "summer, midnight into 1 Jul in Copenhagen")]
    public void Late_evening_cancellations_use_the_Copenhagen_date(
        int year, int month, int day, int hour, int minute, string expected)
    {
        var cancelledAt = Instant.FromUtc(year, month, day, hour, minute);

        Assert.AreEqual(Date(expected), CancellationNotice.EffectiveDate(cancelledAt, CenterTime.Zone));
    }

    [TestMethod]
    public void Cancel_stores_the_instant_and_the_effective_date()
    {
        var member = NewMember();
        var now = Instant.FromUtc(2027, 3, 10, 9, 30);

        Assert.AreEqual(CancellationOutcome.Cancelled, member.Cancel(now, CenterTime.Zone));
        Assert.AreEqual(now, member.CancelledAt);
        Assert.AreEqual(new LocalDate(2027, 4, 30), member.CancellationEffectiveOn);
    }

    [TestMethod]
    public void A_second_cancellation_changes_nothing()
    {
        var member = NewMember();
        var first = Instant.FromUtc(2027, 3, 10, 9, 30);
        member.Cancel(first, CenterTime.Zone);

        var outcome = member.Cancel(Instant.FromUtc(2027, 4, 2, 9, 30), CenterTime.Zone);

        Assert.AreEqual(CancellationOutcome.AlreadyCancelled, outcome);
        Assert.AreEqual(first, member.CancelledAt);
        Assert.AreEqual(new LocalDate(2027, 4, 30), member.CancellationEffectiveOn);
    }

    [TestMethod]
    public void A_new_member_is_not_cancelled()
    {
        var member = NewMember();

        Assert.IsNull(member.CancelledAt);
        Assert.IsNull(member.CancellationEffectiveOn);
    }

    private static Member NewMember() => Member.SignUp(
        Guid.Parse("0199a3c0-0000-7000-8000-00000000abcf"),
        MembershipType.General,
        Consents,
        Instant.FromUtc(2027, 1, 5, 12, 0),
        CenterTime.Zone);

    private static LocalDate Date(string iso) =>
        NodaTime.Text.LocalDatePattern.Iso.Parse(iso).Value;
}
