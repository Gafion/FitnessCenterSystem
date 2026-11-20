using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Tests;

[TestClass]
public sealed class MemberSignUpTests
{
    private static readonly Guid AccountId = Guid.Parse("0199a3c0-0000-7000-8000-00000000abcd");

    private static SignupConsents Consents(bool startNow = false) =>
        new(TermsAccepted: true, "terms-v-test", EarlyStartRequested: startNow, "early-v-test");

    [TestMethod]
    public void New_member_awaits_first_payment_and_has_no_paid_until()
    {
        var member = Member.SignUp(AccountId, MembershipType.General, Consents(), Instant.FromUtc(2026, 3, 10, 12, 0), CenterTime.Zone);

        Assert.AreEqual(MemberStatus.AwaitingFirstPayment, member.Status);
        Assert.IsNull(member.PaidUntil);
        Assert.AreEqual(MembershipType.General.Id, member.MembershipTypeId);
        Assert.AreEqual(AccountId, member.AccountId);
    }

    // Member-since is the date in Copenhagen, not in UTC: late-evening UTC is already
    // the next day in Denmark, and the offset differs between winter (+1) and summer (+2).
    [TestMethod]
    [DataRow(2026, 11, 19, 22, 59, "2026-11-19", DisplayName = "winter, 23:59 in Copenhagen")]
    [DataRow(2026, 11, 19, 23, 0, "2026-11-20", DisplayName = "winter, midnight in Copenhagen")]
    [DataRow(2026, 7, 1, 21, 59, "2026-07-01", DisplayName = "summer, 23:59 in Copenhagen")]
    [DataRow(2026, 7, 1, 22, 0, "2026-07-02", DisplayName = "summer, midnight in Copenhagen")]
    [DataRow(2026, 3, 29, 1, 30, "2026-03-29", DisplayName = "the night clocks go forward")]
    public void Member_since_is_the_signup_date_in_Copenhagen(int year, int month, int day, int hour, int minute, string expected)
    {
        var now = Instant.FromUtc(year, month, day, hour, minute);

        var member = Member.SignUp(AccountId, MembershipType.General, Consents(), now, CenterTime.Zone);

        Assert.AreEqual(expected, member.MemberSince.ToString("uuuu-MM-dd", null));
    }

    [TestMethod]
    public void Accepting_the_terms_records_one_consent_with_version_time_and_channel()
    {
        var now = Instant.FromUtc(2026, 3, 10, 12, 0);

        var member = Member.SignUp(AccountId, MembershipType.General, Consents(), now, CenterTime.Zone);

        var consent = member.Consents.Single();
        Assert.AreEqual(ConsentKind.Terms, consent.Kind);
        Assert.AreEqual("terms-v-test", consent.TextVersion);
        Assert.AreEqual(now, consent.GivenAt);
        Assert.AreEqual(ConsentChannel.Online, consent.Channel);
        Assert.AreEqual(member.Id, consent.MemberId);
    }

    [TestMethod]
    public void Starting_now_also_records_the_express_early_start_request()
    {
        var member = Member.SignUp(AccountId, MembershipType.General, Consents(startNow: true), Instant.FromUtc(2026, 3, 10, 12, 0), CenterTime.Zone);

        var earlyStart = member.Consents.Single(c => c.Kind == ConsentKind.EarlyStart);
        Assert.AreEqual("early-v-test", earlyStart.TextVersion);
        Assert.HasCount(2, member.Consents);
    }

    [TestMethod]
    public void Signup_without_accepting_the_terms_is_refused()
    {
        var consents = new SignupConsents(TermsAccepted: false, "terms-v-test", EarlyStartRequested: false, "early-v-test");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            Member.SignUp(AccountId, MembershipType.General, consents, Instant.FromUtc(2026, 3, 10, 12, 0), CenterTime.Zone));
    }
}
