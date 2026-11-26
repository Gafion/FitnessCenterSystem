using FitnessCenter.Modules.Booking.Domain;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Tests;

/// <summary>
/// Staff moving, editing and cancelling a class (D-068), with a fixed clock.
/// <see cref="ClassChangeProperties"/> checks the rules over many generated scenarios.
/// </summary>
[TestClass]
public sealed class ClassChangeTests
{
    // Tuesday 12 January 2027, 18:00 in Copenhagen; the cancel deadline is 06:00 that day.
    private static readonly Instant Start = Instant.FromUtc(2027, 1, 12, 17, 0);
    private static readonly Instant DayBefore = Start - Duration.FromDays(1);
    private static readonly Duration Length = Duration.FromMinutes(45);
    private static readonly Guid Ann = Guid.Parse("0199a3c0-0000-7000-8000-00000000a001");
    private static readonly Guid Ben = Guid.Parse("0199a3c0-0000-7000-8000-00000000a002");
    private static readonly Guid Cleo = Guid.Parse("0199a3c0-0000-7000-8000-00000000a003");
    private static readonly Guid OtherInstructor = Guid.Parse("0199a3c0-0000-7000-8000-00000000b001");

    [TestMethod]
    public void Moving_keeps_the_bookings_and_the_waitlist_and_names_who_to_tell()
    {
        var session = Session(capacity: 1);
        session.Book(Ann, DayBefore);
        session.JoinWaitlist(Ben, DayBefore);
        var newStart = Start + Duration.FromDays(1);

        var change = Move(session, newStart, DayBefore);

        Assert.AreEqual(ChangeClassOutcome.Changed, change.Outcome);
        Assert.AreEqual(Start, change.MovedFrom);
        CollectionAssert.AreEqual(new[] { Ann }, change.BookedAccounts.ToArray());
        Assert.AreEqual(newStart, session.StartsAt);
        Assert.AreEqual(newStart + Length, session.EndsAt);
        Assert.IsTrue(session.IsBookedBy(Ann));
        Assert.AreEqual(1, session.WaitlistPlaceOf(Ben), "the waitlist stays when a class moves");
    }

    [TestMethod]
    public void After_a_move_a_booked_member_may_cancel_after_the_12_hour_deadline()
    {
        var session = Session(capacity: 2);
        session.Book(Ann, DayBefore);
        var newStart = Start + Duration.FromHours(2);
        Move(session, newStart, DayBefore);

        var oneHourBefore = newStart - Duration.FromHours(1);

        Assert.AreEqual(newStart, session.CancelUntilFor(Ann));
        Assert.AreEqual(CancelOutcome.Cancelled, session.Cancel(Ann, oneHourBefore));
    }

    [TestMethod]
    public void After_a_move_a_booked_member_can_not_cancel_once_the_class_starts()
    {
        var session = Session(capacity: 2);
        session.Book(Ann, DayBefore);
        Move(session, Start + Duration.FromHours(2), DayBefore);

        Assert.AreEqual(CancelOutcome.CancellationClosed, session.Cancel(Ann, session.StartsAt));
    }

    [TestMethod]
    public void A_member_who_books_after_the_move_keeps_the_12_hour_deadline()
    {
        var session = Session(capacity: 2);
        Move(session, Start + Duration.FromHours(2), DayBefore);
        session.Book(Ben, DayBefore + Duration.FromHours(1));

        Assert.AreEqual(session.CancelUntil, session.CancelUntilFor(Ben));
        Assert.AreEqual(CancelOutcome.CancellationClosed, session.Cancel(Ben, session.StartsAt - Duration.FromHours(1)));
    }

    [TestMethod]
    public void Editing_without_a_new_time_is_not_a_move()
    {
        var session = Session(capacity: 2);
        session.Book(Ann, DayBefore);

        var change = session.Change(ClassType.CrossFit.Id, OtherInstructor, Start, Duration.FromMinutes(60), 5, DayBefore);

        Assert.AreEqual(ChangeClassOutcome.Changed, change.Outcome);
        Assert.IsNull(change.MovedFrom);
        Assert.AreEqual(ClassType.CrossFit.Id, session.ClassTypeId);
        Assert.AreEqual(OtherInstructor, session.InstructorAccountId);
        Assert.AreEqual(5, session.Capacity);
        Assert.AreEqual(Duration.FromMinutes(60), session.Length);
        Assert.AreEqual(session.CancelUntil, session.CancelUntilFor(Ann), "the 12-hour deadline stays");
    }

    [TestMethod]
    public void Capacity_can_not_go_below_the_booked_spots()
    {
        var session = Session(capacity: 3);
        session.Book(Ann, DayBefore);
        session.Book(Ben, DayBefore);

        var change = Edit(session, capacity: 1, DayBefore);

        Assert.AreEqual(ChangeClassOutcome.CapacityBelowBookings, change.Outcome);
        Assert.AreEqual(3, session.Capacity, "a refused change changes nothing");
        Assert.AreEqual(ChangeClassOutcome.Changed, Edit(session, capacity: 2, DayBefore).Outcome);
    }

    [TestMethod]
    public void Capacity_can_not_go_below_the_booked_spots_plus_the_held_ones()
    {
        var session = Session(capacity: 2);
        session.Book(Ann, DayBefore);
        session.Book(Ben, DayBefore);
        session.JoinWaitlist(Cleo, DayBefore);
        session.Cancel(Ben, DayBefore);
        session.UpdateOffers(DayBefore);

        Assert.AreEqual(ChangeClassOutcome.CapacityBelowBookings, Edit(session, capacity: 1, DayBefore).Outcome);
    }

    [TestMethod]
    public void More_capacity_lets_the_waitlist_be_offered_the_new_spot()
    {
        var session = Session(capacity: 1);
        session.Book(Ann, DayBefore);
        session.JoinWaitlist(Ben, DayBefore);

        Edit(session, capacity: 2, DayBefore);
        var offered = session.UpdateOffers(DayBefore);

        Assert.AreEqual(Ben, offered.Single().AccountId);
    }

    [TestMethod]
    public void Moving_closer_ends_an_open_offer_by_the_new_cancel_deadline()
    {
        var session = Session(capacity: 1);
        session.Book(Ann, DayBefore);
        session.JoinWaitlist(Ben, DayBefore);
        session.Cancel(Ann, DayBefore);
        session.UpdateOffers(DayBefore);
        var newStart = DayBefore + Duration.FromHours(13);

        Move(session, newStart, DayBefore);

        var offer = session.OpenOfferOf(Ben, DayBefore)!;
        Assert.AreEqual(newStart - Duration.FromHours(12), offer.OfferExpiresAt);
    }

    [TestMethod]
    public void A_class_can_not_move_into_the_past()
    {
        var session = Session(capacity: 1);

        Assert.AreEqual(ChangeClassOutcome.StartsInPast, Move(session, DayBefore, DayBefore).Outcome);
        Assert.AreEqual(Start, session.StartsAt);
    }

    [TestMethod]
    public void A_class_that_has_started_can_not_be_changed_or_cancelled()
    {
        var session = Session(capacity: 1);

        Assert.AreEqual(ChangeClassOutcome.Started, Move(session, Start + Duration.FromDays(1), Start).Outcome);
        Assert.AreEqual(CancelClassOutcome.Started, session.CancelClass(Start).Outcome);
    }

    [TestMethod]
    public void Cancelling_cancels_every_booking_and_clears_the_waitlist()
    {
        var session = Session(capacity: 1);
        session.Book(Ann, DayBefore);
        session.JoinWaitlist(Ben, DayBefore);
        session.JoinWaitlist(Cleo, DayBefore);

        var cancellation = session.CancelClass(DayBefore);

        Assert.AreEqual(CancelClassOutcome.Cancelled, cancellation.Outcome);
        CollectionAssert.AreEqual(new[] { Ann }, cancellation.CancelledBookings.ToArray());
        Assert.IsTrue(session.IsCancelled);
        Assert.AreEqual(DayBefore, session.CancelledAt);
        Assert.AreEqual(0, session.BookedCount);
        Assert.AreEqual(0, session.WaitlistCount);
        Assert.AreEqual(ClassBookingStatus.ClassCancelled, session.Bookings.Single().Status);
        Assert.IsTrue(session.Waitlist.All(entry => entry.Status == WaitlistEntryStatus.ClassCancelled));
    }

    [TestMethod]
    public void Cancelling_ignores_the_12_hour_deadline()
    {
        var session = Session(capacity: 1);
        session.Book(Ann, DayBefore);

        Assert.AreEqual(CancelClassOutcome.Cancelled, session.CancelClass(Start - Duration.FromMinutes(5)).Outcome);
    }

    [TestMethod]
    public void A_cancelled_class_can_not_be_booked_joined_changed_or_offered()
    {
        var session = Session(capacity: 1);
        session.Book(Ann, DayBefore);
        session.JoinWaitlist(Ben, DayBefore);
        session.CancelClass(DayBefore);

        Assert.AreEqual(BookOutcome.ClassCancelled, session.Book(Cleo, DayBefore));
        Assert.AreEqual(JoinWaitlistOutcome.ClassCancelled, session.JoinWaitlist(Cleo, DayBefore));
        Assert.AreEqual(ChangeClassOutcome.ClassCancelled, Edit(session, capacity: 5, DayBefore).Outcome);
        Assert.AreEqual(CancelClassOutcome.AlreadyCancelled, session.CancelClass(DayBefore).Outcome);
        Assert.IsEmpty(session.UpdateOffers(DayBefore));
    }

    [TestMethod]
    public void Class_type_names_are_1_to_100_characters_without_outer_spaces()
    {
        Assert.IsTrue(ClassType.IsValidName("Yoga"));
        Assert.IsTrue(ClassType.IsValidName(new string('a', 100)));
        Assert.IsFalse(ClassType.IsValidName(new string('a', 101)));
        Assert.IsFalse(ClassType.IsValidName(""));
        Assert.IsFalse(ClassType.IsValidName("   "));
        Assert.IsFalse(ClassType.IsValidName(" Yoga"));
        Assert.IsFalse(ClassType.IsValidName(null));
        Assert.ThrowsExactly<ArgumentException>(() => ClassType.Create(" "));
    }

    [TestMethod]
    public void A_class_type_is_created_and_renamed()
    {
        var type = ClassType.Create("Yoga");

        type.Rename("Hot yoga");

        Assert.AreEqual("Hot yoga", type.Name);
        Assert.AreNotEqual(Guid.Empty, type.Id.Value);
    }

    private static ClassChange Move(ClassSession session, Instant startsAt, Instant now) =>
        session.Change(session.ClassTypeId, session.InstructorAccountId, startsAt, session.Length, session.Capacity, now);

    private static ClassChange Edit(ClassSession session, int capacity, Instant now) =>
        session.Change(session.ClassTypeId, session.InstructorAccountId, session.StartsAt, session.Length, capacity, now);

    private static ClassSession Session(int capacity) =>
        ClassSession.Schedule(ClassType.Spinning, TestInstructor.Id, Start, Length, capacity);
}
