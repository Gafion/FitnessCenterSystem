using FitnessCenter.Modules.Booking.Domain;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Tests;

/// <summary>Examples of the waitlist rules (D-051). <see cref="WaitlistProperties"/> checks them over generated scenarios.</summary>
[TestClass]
public sealed class WaitlistTests
{
    private static readonly Instant Start = Instant.FromUtc(2027, 1, 12, 17, 0);
    private static readonly Instant DayBefore = Start - Duration.FromDays(1);
    private static readonly Guid Ann = Guid.Parse("0199a3c0-0000-7000-8000-00000000a001");
    private static readonly Guid Ben = Guid.Parse("0199a3c0-0000-7000-8000-00000000a002");
    private static readonly Guid Cleo = Guid.Parse("0199a3c0-0000-7000-8000-00000000a003");
    private static readonly Guid Dan = Guid.Parse("0199a3c0-0000-7000-8000-00000000a004");

    [TestMethod]
    public void Members_queue_for_a_full_class_first_come_first_served()
    {
        var session = FullSession();

        Assert.AreEqual(JoinWaitlistOutcome.Joined, session.JoinWaitlist(Ben, DayBefore));
        Assert.AreEqual(JoinWaitlistOutcome.Joined, session.JoinWaitlist(Cleo, DayBefore + Duration.FromMinutes(1)));

        Assert.AreEqual(1, session.WaitlistPlaceOf(Ben));
        Assert.AreEqual(2, session.WaitlistPlaceOf(Cleo));
        Assert.IsNull(session.WaitlistPlaceOf(Dan));
    }

    [TestMethod]
    public void Leaving_moves_everyone_behind_up_one_place()
    {
        var session = FullSession();
        session.JoinWaitlist(Ben, DayBefore);
        session.JoinWaitlist(Cleo, DayBefore + Duration.FromMinutes(1));
        session.JoinWaitlist(Dan, DayBefore + Duration.FromMinutes(2));

        Assert.AreEqual(LeaveWaitlistOutcome.Left, session.LeaveWaitlist(Ben, DayBefore + Duration.FromMinutes(3)));

        Assert.IsNull(session.WaitlistPlaceOf(Ben));
        Assert.AreEqual(1, session.WaitlistPlaceOf(Cleo));
        Assert.AreEqual(2, session.WaitlistPlaceOf(Dan));
    }

    [TestMethod]
    public void A_member_who_leaves_and_rejoins_goes_to_the_back()
    {
        var session = FullSession();
        session.JoinWaitlist(Ben, DayBefore);
        session.JoinWaitlist(Cleo, DayBefore + Duration.FromMinutes(1));
        session.LeaveWaitlist(Ben, DayBefore + Duration.FromMinutes(2));

        Assert.AreEqual(JoinWaitlistOutcome.Joined, session.JoinWaitlist(Ben, DayBefore + Duration.FromMinutes(3)));

        Assert.AreEqual(1, session.WaitlistPlaceOf(Cleo));
        Assert.AreEqual(2, session.WaitlistPlaceOf(Ben));
    }

    [TestMethod]
    public void A_member_cannot_join_the_waitlist_twice()
    {
        var session = FullSession();
        session.JoinWaitlist(Ben, DayBefore);

        Assert.AreEqual(JoinWaitlistOutcome.AlreadyWaitlisted, session.JoinWaitlist(Ben, DayBefore));
        Assert.HasCount(1, session.Queue);
    }

    [TestMethod]
    public void A_booked_member_cannot_join_the_waitlist()
    {
        var session = FullSession();

        Assert.AreEqual(JoinWaitlistOutcome.AlreadyBooked, session.JoinWaitlist(Ann, DayBefore));
        Assert.IsEmpty(session.Queue);
    }

    [TestMethod]
    public void A_class_with_free_spots_has_no_waitlist()
    {
        var session = Session(capacity: 2);
        session.Book(Ann, DayBefore);

        Assert.AreEqual(JoinWaitlistOutcome.NotFull, session.JoinWaitlist(Ben, DayBefore));
        Assert.IsEmpty(session.Queue);
    }

    [TestMethod]
    public void The_waitlist_follows_the_booking_window()
    {
        var session = FullSession();

        Assert.AreEqual(
            JoinWaitlistOutcome.NotOpenYet,
            session.JoinWaitlist(Ben, Start - BookingPolicy.BookingWindow - Duration.FromSeconds(1)));
        Assert.AreEqual(JoinWaitlistOutcome.Started, session.JoinWaitlist(Ben, Start));
        Assert.IsEmpty(session.Queue);
    }

    [TestMethod]
    public void A_member_whose_membership_ends_before_the_class_cannot_join()
    {
        var session = FullSession();

        Assert.AreEqual(
            JoinWaitlistOutcome.AfterMembershipEnds,
            session.JoinWaitlist(Ben, DayBefore, lastDayOfAccess: new LocalDate(2027, 1, 11)));
        Assert.AreEqual(
            JoinWaitlistOutcome.Joined,
            session.JoinWaitlist(Ben, DayBefore, lastDayOfAccess: new LocalDate(2027, 1, 12)));
    }

    [TestMethod]
    public void Leaving_a_waitlist_one_is_not_on_says_so()
    {
        var session = FullSession();

        Assert.AreEqual(LeaveWaitlistOutcome.NotWaitlisted, session.LeaveWaitlist(Ben, DayBefore));
    }

    [TestMethod]
    public void Booking_a_freed_spot_takes_the_member_out_of_the_queue()
    {
        var session = FullSession();
        session.JoinWaitlist(Ben, DayBefore);
        session.JoinWaitlist(Cleo, DayBefore + Duration.FromMinutes(1));
        session.Cancel(Ann, DayBefore + Duration.FromMinutes(2));

        Assert.AreEqual(BookOutcome.Booked, session.Book(Ben, DayBefore + Duration.FromMinutes(3)));

        Assert.IsTrue(session.IsBookedBy(Ben));
        Assert.IsNull(session.WaitlistPlaceOf(Ben));
        Assert.AreEqual(1, session.WaitlistPlaceOf(Cleo));
        Assert.AreEqual(WaitlistEntryStatus.Booked, session.Waitlist.Single(e => e.AccountId == Ben).Status);
    }

    /// <summary>Equal join times keep the order the members joined in.</summary>
    [TestMethod]
    public void Members_who_join_at_the_same_instant_keep_their_order()
    {
        var session = FullSession();
        session.JoinWaitlist(Ben, DayBefore);
        session.JoinWaitlist(Cleo, DayBefore);
        session.JoinWaitlist(Dan, DayBefore);

        CollectionAssert.AreEqual(new[] { Ben, Cleo, Dan }, session.Queue.Select(e => e.AccountId).ToArray());
    }

    /// <summary>A one-spot class that <see cref="Ann"/> has booked.</summary>
    private static ClassSession FullSession()
    {
        var session = Session(capacity: 1);
        session.Book(Ann, DayBefore - Duration.FromDays(1));
        return session;
    }

    private static ClassSession Session(int capacity) =>
        ClassSession.Schedule(ClassType.Spinning, TestInstructor.Id, Start, Duration.FromMinutes(50), capacity);
}
