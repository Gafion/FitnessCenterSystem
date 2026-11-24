using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Tests;

/// <summary>Examples of the booking rules (D-039). <see cref="ClassSessionProperties"/> checks them over many generated times.</summary>
[TestClass]
public sealed class ClassSessionTests
{
    private static readonly Instant Start = Instant.FromUtc(2027, 1, 12, 17, 0);
    private static readonly Guid Ann = Guid.Parse("0199a3c0-0000-7000-8000-00000000a001");
    private static readonly Guid Ben = Guid.Parse("0199a3c0-0000-7000-8000-00000000a002");

    [TestMethod]
    public void A_member_books_a_spot()
    {
        var session = Session(capacity: 2);

        Assert.AreEqual(BookOutcome.Booked, session.Book(Ann, Start - Duration.FromDays(1)));
        Assert.AreEqual(1, session.SpotsLeft);
        Assert.IsTrue(session.IsBookedBy(Ann));
    }

    [TestMethod]
    public void A_full_class_refuses_more_bookings()
    {
        var session = Session(capacity: 1);
        session.Book(Ann, Start - Duration.FromDays(1));

        Assert.AreEqual(BookOutcome.Full, session.Book(Ben, Start - Duration.FromDays(1)));
    }

    [TestMethod]
    public void A_member_cannot_book_the_same_class_twice()
    {
        var session = Session(capacity: 5);
        session.Book(Ann, Start - Duration.FromDays(1));

        Assert.AreEqual(BookOutcome.AlreadyBooked, session.Book(Ann, Start - Duration.FromDays(1)));
        Assert.AreEqual(4, session.SpotsLeft);
    }

    [TestMethod]
    public void Booking_opens_14_days_before_the_start_and_closes_at_the_start()
    {
        var session = Session(capacity: 5);

        Assert.AreEqual(BookOutcome.NotOpenYet, session.Book(Ann, Start - Duration.FromDays(14) - Duration.FromSeconds(1)));
        Assert.AreEqual(BookOutcome.Booked, session.Book(Ann, Start - Duration.FromDays(14)));
        Assert.AreEqual(BookOutcome.Started, session.Book(Ben, Start));
    }

    [TestMethod]
    public void Cancelling_frees_the_spot_and_the_member_may_book_again()
    {
        var session = Session(capacity: 1);
        session.Book(Ann, Start - Duration.FromDays(2));

        Assert.AreEqual(CancelOutcome.Cancelled, session.Cancel(Ann, Start - Duration.FromDays(1)));
        Assert.AreEqual(1, session.SpotsLeft);
        Assert.AreEqual(BookOutcome.Booked, session.Book(Ben, Start - Duration.FromDays(1)));
        Assert.AreEqual(BookOutcome.Full, session.Book(Ann, Start - Duration.FromDays(1)));
    }

    [TestMethod]
    public void Cancelling_closes_12_hours_before_the_start()
    {
        var session = Session(capacity: 5);
        session.Book(Ann, Start - Duration.FromDays(1));
        session.Book(Ben, Start - Duration.FromDays(1));

        Assert.AreEqual(CancelOutcome.Cancelled, session.Cancel(Ann, Start - Duration.FromHours(12)));
        Assert.AreEqual(
            CancelOutcome.CancellationClosed,
            session.Cancel(Ben, Start - Duration.FromHours(12) + Duration.FromSeconds(1)));
        Assert.IsTrue(session.IsBookedBy(Ben));
    }

    [TestMethod]
    public void Cancelling_without_a_booking_says_so()
    {
        Assert.AreEqual(CancelOutcome.NotBooked, Session(capacity: 5).Cancel(Ann, Start - Duration.FromDays(1)));
    }

    /// <summary>
    /// The cut-off is 12 real hours. Across the switch to summer time (29 March 2026,
    /// 02:00 → 03:00 in Copenhagen) that is 13 hours on the wall clock.
    /// </summary>
    [TestMethod]
    public void The_cutoff_counts_real_hours_across_daylight_saving()
    {
        var start = new LocalDateTime(2026, 3, 29, 9, 0).InZoneStrictly(CenterTime.Zone).ToInstant();
        var session = ClassSession.Schedule(ClassType.Spinning, "Test Instructor", start, Duration.FromMinutes(50), capacity: 5);

        var cancelUntil = session.CancelUntil.InZone(CenterTime.Zone).LocalDateTime;

        Assert.AreEqual(new LocalDateTime(2026, 3, 28, 20, 0), cancelUntil);
    }

    // D-055 option A: Start is 18:00 on 12 January 2027 in Copenhagen.
    [TestMethod]
    [DataRow(2027, 1, 13, "Booked", DisplayName = "the class is the day before the last day of access")]
    [DataRow(2027, 1, 12, "Booked", DisplayName = "the class is on the last day of access")]
    [DataRow(2027, 1, 11, "AfterMembershipEnds", DisplayName = "the class is the day after the last day of access")]
    public void A_cancelled_member_books_only_classes_up_to_the_last_day_of_access(
        int year, int month, int day, string expected)
    {
        var session = Session(capacity: 5);

        var outcome = session.Book(Ann, Start - Duration.FromDays(1), new LocalDate(year, month, day));

        Assert.AreEqual(expected, outcome.ToString());
        Assert.AreEqual(expected == "Booked", session.IsBookedBy(Ann));
    }

    [TestMethod]
    public void A_refused_booking_after_the_membership_ends_takes_no_spot()
    {
        var session = Session(capacity: 1);

        Assert.AreEqual(BookOutcome.AfterMembershipEnds, session.Book(Ann, Start - Duration.FromDays(1), new LocalDate(2027, 1, 11)));
        Assert.AreEqual(1, session.SpotsLeft);
    }

    [TestMethod]
    public void The_last_day_of_access_is_a_Copenhagen_date()
    {
        // 23:30 UTC on 12 January is 00:30 on 13 January in Copenhagen (UTC+1).
        var lateStart = Instant.FromUtc(2027, 1, 12, 23, 30);
        var session = ClassSession.Schedule(ClassType.Spinning, "Test Instructor", lateStart, Duration.FromMinutes(50), 5);

        Assert.IsTrue(session.StartsAfter(new LocalDate(2027, 1, 12)));
        Assert.IsFalse(session.StartsAfter(new LocalDate(2027, 1, 13)));
        Assert.IsFalse(session.StartsAfter(null));
    }

    private static ClassSession Session(int capacity) =>
        ClassSession.Schedule(ClassType.Spinning, "Test Instructor", Start, Duration.FromMinutes(50), capacity);
}
