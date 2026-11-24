using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Tests;

/// <summary>The 18+ rule at signup (D-052), with a fixed instant in place of the clock.</summary>
[TestClass]
public sealed class MinimumAgeTests
{
    private static readonly Guid AccountId = Guid.Parse("0199a3c0-0000-7000-8000-00000000abce");

    private static readonly SignupConsents Consents =
        new(TermsAccepted: true, "terms-v-test", EarlyStartRequested: false, "early-v-test");

    [TestMethod]
    [DataRow("2008-11-24", "2026-11-23", false, DisplayName = "the day before the 18th birthday")]
    [DataRow("2008-11-24", "2026-11-24", true, DisplayName = "on the 18th birthday")]
    [DataRow("2008-11-24", "2026-11-25", true, DisplayName = "the day after the 18th birthday")]
    [DataRow("2008-12-31", "2026-12-31", true, DisplayName = "18th birthday on New Year's Eve")]
    [DataRow("2009-01-01", "2026-12-31", false, DisplayName = "born on New Year's Day, one day short")]
    [DataRow("2030-01-01", "2026-11-24", false, DisplayName = "born in the future")]
    public void Eighteen_is_reached_on_the_18th_birthday(string born, string today, bool expected)
    {
        Assert.AreEqual(expected, MinimumAge.IsMetOn(Date(born), Date(today)));
    }

    // UNVERIFIED (FIT-67): no official Danish source found for 29 February; the stricter rule
    // applies. 18 years after a leap year is never a leap year, so someone born on 29 February
    // always turns 18 on 1 March and is refused on 28 February.
    [TestMethod]
    [DataRow("2008-02-29", "2026-02-28", false, DisplayName = "born 29 Feb: refused on 28 Feb")]
    [DataRow("2008-02-29", "2026-03-01", true, DisplayName = "born 29 Feb: 18 on 1 Mar")]
    [DataRow("2008-02-28", "2026-02-28", true, DisplayName = "born 28 Feb: 18 on 28 Feb")]
    [DataRow("2008-03-01", "2026-02-28", false, DisplayName = "born 1 Mar: refused on 28 Feb")]
    [DataRow("2008-03-01", "2026-03-01", true, DisplayName = "born 1 Mar: 18 on 1 Mar")]
    public void Born_on_29_February_turns_18_on_1_March(string born, string today, bool expected)
    {
        Assert.AreEqual(expected, MinimumAge.IsMetOn(Date(born), Date(today)));
    }

    [TestMethod]
    public void Born_on_29_February_signing_up_at_midnight_in_Copenhagen()
    {
        var born = new LocalDate(2008, 2, 29);

        // 28 Feb 2026, 23:59 in Copenhagen (UTC+1): still 17.
        AssertSignup(born, Instant.FromUtc(2026, 2, 28, 22, 59), allowed: false);
        // 1 Mar 2026, 00:00 in Copenhagen, while UTC is still 28 Feb.
        AssertSignup(born, Instant.FromUtc(2026, 2, 28, 23, 0), allowed: true);
    }

    // The age is checked on the signup date in Copenhagen, not in UTC: late-evening UTC is
    // already the next day in Denmark, and the offset is +1 in winter and +2 in summer.
    [TestMethod]
    [DataRow(2026, 11, 23, 22, 59, false, DisplayName = "winter, 23:59 in Copenhagen the day before")]
    [DataRow(2026, 11, 23, 23, 0, true, DisplayName = "winter, midnight in Copenhagen on the birthday")]
    public void Winter_signup_uses_the_Copenhagen_date(int year, int month, int day, int hour, int minute, bool allowed)
    {
        AssertSignup(new LocalDate(2008, 11, 24), Instant.FromUtc(year, month, day, hour, minute), allowed);
    }

    [TestMethod]
    [DataRow(2026, 6, 30, 21, 59, false, DisplayName = "summer, 23:59 in Copenhagen the day before")]
    [DataRow(2026, 6, 30, 22, 0, true, DisplayName = "summer, midnight in Copenhagen on the birthday")]
    public void Summer_signup_uses_the_Copenhagen_date(int year, int month, int day, int hour, int minute, bool allowed)
    {
        AssertSignup(new LocalDate(2008, 7, 1), Instant.FromUtc(year, month, day, hour, minute), allowed);
    }

    [TestMethod]
    public void Signup_stores_the_date_of_birth()
    {
        var born = new LocalDate(1990, 5, 17);

        var member = Member.SignUp(
            AccountId, MembershipType.General, Consents, Instant.FromUtc(2026, 11, 24, 12, 0), CenterTime.Zone, born);

        Assert.AreEqual(born, member.DateOfBirth);
    }

    [TestMethod]
    public void Signup_without_a_date_of_birth_stores_none()
    {
        // Google signups give no date of birth yet (FIT-68).
        var member = Member.SignUp(
            AccountId, MembershipType.General, Consents, Instant.FromUtc(2026, 11, 24, 12, 0), CenterTime.Zone);

        Assert.IsNull(member.DateOfBirth);
    }

    private static void AssertSignup(LocalDate born, Instant now, bool allowed)
    {
        Member Act() => Member.SignUp(AccountId, MembershipType.General, Consents, now, CenterTime.Zone, born);

        if (allowed)
        {
            Assert.AreEqual(born, Act().DateOfBirth);
        }
        else
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => Act());
        }
    }

    private static LocalDate Date(string iso) =>
        NodaTime.Text.LocalDatePattern.Iso.Parse(iso).Value;
}
