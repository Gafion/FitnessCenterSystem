using CsCheck;
using FitnessCenter.Modules.Booking.Domain;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Tests;

/// <summary>
/// Property tests (D-036): the booking rules (D-039) hold for any time and any order of
/// bookings and cancellations, not only the examples in <see cref="ClassSessionTests"/>.
/// </summary>
[TestClass]
public sealed class ClassSessionProperties
{
    private static readonly Instant Start = Instant.FromUtc(2027, 1, 12, 17, 0);

    /// <summary>
    /// A moment from 30 days before to 1 day after the class starts, to the second. Most
    /// samples land within 10 minutes of a rule's edge (the start, the cancel cut-off, the
    /// booking window opening), because uniform times over a month almost never hit an edge.
    /// </summary>
    private static readonly Gen<Instant> AroundStart = Gen.OneOf(
        Gen.Long[-30 * 86_400, 86_400].Select(seconds => Start + Duration.FromSeconds(seconds)),
        Near(Start),
        Near(Start - BookingPolicy.CancellationCutoff),
        Near(Start - BookingPolicy.BookingWindow));

    private static Gen<Instant> Near(Instant edge) =>
        Gen.OneOf(
            Gen.Long[-600, 600].Select(seconds => edge + Duration.FromSeconds(seconds)),
            Gen.Long[-1, 1].Select(seconds => edge + Duration.FromSeconds(seconds)));

    [TestMethod]
    public void Booking_succeeds_exactly_inside_the_booking_window()
    {
        AroundStart.Sample(now =>
        {
            var expected = now < Start && Start - now <= BookingPolicy.BookingWindow;
            return (Session(capacity: 1).Book(Guid.NewGuid(), now) == BookOutcome.Booked) == expected;
        });
    }

    [TestMethod]
    public void Cancelling_succeeds_exactly_until_12_hours_before_the_start()
    {
        AroundStart.Sample(now =>
        {
            var session = Session(capacity: 1);
            var account = Guid.NewGuid();
            session.Book(account, Start - BookingPolicy.BookingWindow);

            var expected = now <= Start - BookingPolicy.CancellationCutoff;
            return (session.Cancel(account, now) == CancelOutcome.Cancelled) == expected;
        });
    }

    /// <summary>
    /// Any sequence of bookings and cancellations by a few members: never more active
    /// bookings than spots, and never two active bookings for one member.
    /// </summary>
    [TestMethod]
    public void Bookings_never_exceed_capacity_or_double_up()
    {
        var accounts = Enumerable.Range(1, 6).Select(n => new Guid(n, 0, 0, new byte[8])).ToArray();
        var step = Gen.Select(Gen.Bool, Gen.Int[0, accounts.Length - 1], Gen.Long[-15 * 86_400, 0]);
        var scenario = Gen.Select(Gen.Int[1, 4], step.Array[0, 40]);

        scenario.Sample((capacity, steps) =>
        {
            var session = Session(capacity);
            foreach (var (book, who, offsetSeconds) in steps)
            {
                var now = Start + Duration.FromSeconds(offsetSeconds);
                if (book)
                {
                    session.Book(accounts[who], now);
                }
                else
                {
                    session.Cancel(accounts[who], now);
                }
            }

            var active = session.Bookings.Where(b => b.IsActive).ToList();
            return active.Count <= capacity
                && session.SpotsLeft == capacity - active.Count
                && active.Select(b => b.AccountId).Distinct().Count() == active.Count;
        });
    }

    private static ClassSession Session(int capacity) =>
        ClassSession.Schedule(ClassType.Spinning, "Test Instructor", Start, Duration.FromMinutes(50), capacity);
}
