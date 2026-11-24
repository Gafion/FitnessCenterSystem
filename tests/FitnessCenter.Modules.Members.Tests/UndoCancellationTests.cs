using CsCheck;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Tests;

/// <summary>
/// Undoing a pending cancellation (D-055, FIT-95): allowed up to and including the effective
/// date, in Copenhagen. Fixed instants stand in for the clock.
/// </summary>
[TestClass]
public sealed class UndoCancellationTests
{
    private static readonly SignupConsents Consents =
        new(TermsAccepted: true, "terms-v-test", EarlyStartRequested: false, "early-v-test");

    // D-048's example: cancelled on 10 March 2027, effective at the end of 30 April 2027.
    private static readonly Instant CancelledAt = Instant.FromUtc(2027, 3, 10, 9, 30);

    [TestMethod]
    public void Undoing_clears_the_cancellation_and_keeps_the_record()
    {
        var member = CancelledMember();
        var undoneAt = Instant.FromUtc(2027, 3, 20, 12, 0);

        Assert.AreEqual(UndoCancellationOutcome.Undone, member.UndoCancellation(undoneAt, CenterTime.Zone));

        Assert.IsNull(member.CancelledAt);
        Assert.IsNull(member.CancellationEffectiveOn);
        Assert.IsTrue(member.HasAccessOn(new LocalDate(2027, 5, 1)));
        var record = member.Cancellations.Single();
        Assert.AreEqual(CancelledAt, record.CancelledAt);
        Assert.AreEqual(new LocalDate(2027, 4, 30), record.EffectiveOn);
        Assert.AreEqual(undoneAt, record.UndoneAt);
    }

    // 30 April 23:59 in Copenhagen is 21:59 UTC (summer time, +2); midnight is 22:00 UTC.
    [TestMethod]
    [DataRow(2027, 4, 30, 21, 59, true, DisplayName = "23:59 on the effective date in Copenhagen")]
    [DataRow(2027, 4, 30, 22, 0, false, DisplayName = "midnight after the effective date in Copenhagen")]
    [DataRow(2027, 3, 10, 9, 31, true, DisplayName = "right after cancelling")]
    [DataRow(2027, 6, 1, 10, 0, false, DisplayName = "a month after the effective date")]
    public void Undo_is_allowed_up_to_and_including_the_effective_date(
        int year, int month, int day, int hour, int minute, bool allowed)
    {
        var member = CancelledMember();

        var outcome = member.UndoCancellation(Instant.FromUtc(year, month, day, hour, minute), CenterTime.Zone);

        Assert.AreEqual(allowed ? UndoCancellationOutcome.Undone : UndoCancellationOutcome.AlreadyEffective, outcome);
    }

    [TestMethod]
    public void A_refused_undo_changes_nothing()
    {
        var member = CancelledMember();

        member.UndoCancellation(Instant.FromUtc(2027, 5, 1, 10, 0), CenterTime.Zone);

        Assert.AreEqual(CancelledAt, member.CancelledAt);
        Assert.AreEqual(new LocalDate(2027, 4, 30), member.CancellationEffectiveOn);
        Assert.IsNull(member.Cancellations.Single().UndoneAt);
    }

    [TestMethod]
    public void Undoing_with_nothing_pending_changes_nothing()
    {
        var member = NewMember();

        var outcome = member.UndoCancellation(Instant.FromUtc(2027, 3, 20, 12, 0), CenterTime.Zone);

        Assert.AreEqual(UndoCancellationOutcome.NothingPending, outcome);
        Assert.IsNull(member.CancelledAt);
        Assert.IsEmpty(member.Cancellations);
    }

    [TestMethod]
    public void A_second_undo_changes_nothing()
    {
        var member = CancelledMember();
        var undoneAt = Instant.FromUtc(2027, 3, 20, 12, 0);
        member.UndoCancellation(undoneAt, CenterTime.Zone);

        var outcome = member.UndoCancellation(Instant.FromUtc(2027, 3, 21, 12, 0), CenterTime.Zone);

        Assert.AreEqual(UndoCancellationOutcome.NothingPending, outcome);
        Assert.AreEqual(undoneAt, member.Cancellations.Single().UndoneAt);
    }

    [TestMethod]
    public void Cancelling_again_after_an_undo_starts_a_new_cancellation_and_keeps_the_old_one()
    {
        var member = CancelledMember();
        member.UndoCancellation(Instant.FromUtc(2027, 3, 20, 12, 0), CenterTime.Zone);
        var again = Instant.FromUtc(2027, 4, 5, 12, 0);

        Assert.AreEqual(CancellationOutcome.Cancelled, member.Cancel(again, CenterTime.Zone));

        Assert.AreEqual(again, member.CancelledAt);
        Assert.AreEqual(new LocalDate(2027, 5, 31), member.CancellationEffectiveOn);
        Assert.HasCount(2, member.Cancellations);
        Assert.IsNotNull(member.Cancellations[0].UndoneAt);
        Assert.IsNull(member.Cancellations[1].UndoneAt);
    }

    [TestMethod]
    public void Cancelling_adds_a_record_to_the_history()
    {
        var member = CancelledMember();

        var record = member.Cancellations.Single();
        Assert.AreEqual(CancelledAt, record.CancelledAt);
        Assert.AreEqual(new LocalDate(2027, 4, 30), record.EffectiveOn);
        Assert.IsNull(record.UndoneAt);
    }

    /// <summary>For any cancellation and any later instant, undo works exactly while the Copenhagen day is on or before the effective date.</summary>
    [TestMethod]
    public void Undo_is_allowed_exactly_until_the_effective_date_ends() =>
        Gen.Select(Gen.Long[0, 10L * 365 * 86_400], Gen.Long[0, 120L * 86_400]).Sample((offset, wait) =>
        {
            var cancelledAt = Instant.FromUtc(2026, 1, 1, 0, 0).Plus(Duration.FromSeconds(offset));
            var undoAt = cancelledAt.Plus(Duration.FromSeconds(wait));
            var member = NewMember();
            member.Cancel(cancelledAt, CenterTime.Zone);
            var effectiveOn = member.CancellationEffectiveOn!.Value;

            var outcome = member.UndoCancellation(undoAt, CenterTime.Zone);

            var allowed = undoAt.InZone(CenterTime.Zone).Date <= effectiveOn;
            return outcome == (allowed ? UndoCancellationOutcome.Undone : UndoCancellationOutcome.AlreadyEffective)
                && (member.CancelledAt is null) == allowed;
        });

    private static Member CancelledMember()
    {
        var member = NewMember();
        member.Cancel(CancelledAt, CenterTime.Zone);
        return member;
    }

    private static Member NewMember() => Member.SignUp(
        Guid.Parse("0199a3c0-0000-7000-8000-00000000abd0"),
        MembershipType.General,
        Consents,
        Instant.FromUtc(2025, 12, 5, 12, 0),
        CenterTime.Zone);
}
