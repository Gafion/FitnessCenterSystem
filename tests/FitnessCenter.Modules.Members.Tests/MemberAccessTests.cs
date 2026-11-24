using CsCheck;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Tests;

/// <summary>
/// Access after a cancellation (D-048, D-055): the member keeps access up to and including the
/// effective date, and not after. Dates are Europe/Copenhagen.
/// </summary>
[TestClass]
public sealed class MemberAccessTests
{
    private static readonly Guid AccountId = Guid.Parse("0199a3c0-0000-7000-8000-00000000abce");

    /// <summary>D-048's example: cancelled on 10 March, effective at the end of 30 April.</summary>
    private static readonly Instant CancelledAt = Instant.FromUtc(2027, 3, 10, 12, 0);

    private static Member NewMember() =>
        Member.SignUp(
            AccountId,
            MembershipType.General,
            new SignupConsents(TermsAccepted: true, "terms-v-test", EarlyStartRequested: false, "early-v-test"),
            Instant.FromUtc(2027, 1, 5, 12, 0),
            CenterTime.Zone);

    [TestMethod]
    public void A_member_who_has_not_cancelled_has_access_on_any_day()
    {
        var member = NewMember();

        Assert.IsTrue(member.HasAccessOn(new LocalDate(2027, 1, 5)));
        Assert.IsTrue(member.HasAccessOn(new LocalDate(2040, 12, 31)));
    }

    [TestMethod]
    [DataRow(2027, 4, 29, true, DisplayName = "the day before the effective date")]
    [DataRow(2027, 4, 30, true, DisplayName = "the effective date itself")]
    [DataRow(2027, 5, 1, false, DisplayName = "the day after the effective date")]
    [DataRow(2027, 3, 10, true, DisplayName = "the day of the cancellation")]
    public void A_cancelled_member_has_access_until_the_effective_date(int year, int month, int day, bool expected)
    {
        var member = NewMember();
        member.Cancel(CancelledAt, CenterTime.Zone);

        Assert.AreEqual(new LocalDate(2027, 4, 30), member.CancellationEffectiveOn);
        Assert.AreEqual(expected, member.HasAccessOn(new LocalDate(year, month, day)));
    }

    [TestMethod]
    public void Access_ends_on_any_day_after_the_effective_date_and_not_before()
    {
        var days = Gen.Int[0, 3650].Select(d => new LocalDate(2024, 1, 1).PlusDays(d));
        Gen.Select(days, days).Sample((cancelledOn, day) =>
        {
            var member = NewMember();
            member.Cancel(CenterTime.Zone.AtStartOfDay(cancelledOn).ToInstant(), CenterTime.Zone);
            var effective = member.CancellationEffectiveOn!.Value;

            return member.HasAccessOn(day) == (day <= effective);
        });
    }
}
