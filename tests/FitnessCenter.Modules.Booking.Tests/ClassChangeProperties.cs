using CsCheck;
using FitnessCenter.Modules.Booking.Domain;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Tests;

/// <summary>
/// Property tests (D-036) for staff changes to a class (D-068). Over any sequence of member
/// actions mixed with moves, capacity changes and a cancellation, with the offers brought up to
/// date around every step as the handlers do: the class is never overfilled, no open offer
/// runs past the cancel deadline, a cancelled class keeps no booking or waitlist entry, and a
/// member whose class moved can cancel until it starts.
/// </summary>
[TestClass]
public sealed class ClassChangeProperties
{
    private static readonly Instant Start = Instant.FromUtc(2027, 1, 12, 17, 0);

    private enum Action
    {
        Book,
        Cancel,
        Join,
        Decline,
        Move,
        Resize,
        CancelClass,
    }

    private static readonly Guid[] Accounts =
        Enumerable.Range(1, 5).Select(n => new Guid(n, 0, 0, new byte[8])).ToArray();

    /// <summary>
    /// Steps at increasing times from 3 days before the class, up to 3 hours apart. A move
    /// shifts the class by −48 to +48 hours; a resize sets the capacity to 1–4.
    /// </summary>
    private static readonly Gen<(int Capacity, (Action Action, int Who, int Minutes, int Shift)[] Steps)> Scenario =
        Gen.Select(
            Gen.Int[1, 3],
            Gen.Select(Gen.Enum<Action>(), Gen.Int[0, Accounts.Length - 1], Gen.Int[0, 180], Gen.Int[-48, 48]).Array[0, 50]);

    [TestMethod]
    public void Staff_changes_keep_the_booking_rules()
    {
        Scenario.Sample((capacity, steps) =>
        {
            var session = ClassSession.Schedule(ClassType.Spinning, TestInstructor.Id, Start, Duration.FromMinutes(50), capacity);
            var now = Start - Duration.FromDays(3);

            foreach (var (action, who, minutes, shift) in steps)
            {
                now += Duration.FromMinutes(minutes);
                var account = Accounts[who];
                session.UpdateOffers(now);
                var capacityBefore = session.Capacity;
                switch (action)
                {
                    case Action.Book:
                        session.Book(account, now);
                        break;
                    case Action.Cancel:
                        session.Cancel(account, now);
                        break;
                    case Action.Join:
                        session.JoinWaitlist(account, now);
                        break;
                    case Action.Decline:
                        session.DeclineOffer(account, now);
                        break;
                    case Action.Move:
                        var booked = session.Bookings.Where(b => b.IsActive).Select(b => b.AccountId).ToList();
                        var moved = session.Change(
                            session.ClassTypeId, session.InstructorAccountId, session.StartsAt + Duration.FromHours(shift),
                            session.Length, session.Capacity, now);
                        if (moved is { Outcome: ChangeClassOutcome.Changed, MovedFrom: not null }
                            && booked.Any(a => session.CancelUntilFor(a) != session.StartsAt))
                        {
                            return false;
                        }

                        break;
                    case Action.Resize:
                        var resized = session.Change(
                            session.ClassTypeId, session.InstructorAccountId, session.StartsAt, session.Length, 1 + (shift & 3), now);
                        if (resized.Outcome == ChangeClassOutcome.CapacityBelowBookings && session.Capacity != capacityBefore)
                        {
                            return false;
                        }

                        break;
                    case Action.CancelClass:
                        session.CancelClass(now);
                        break;
                }

                session.UpdateOffers(now);
                if (!Holds(session, now))
                {
                    return false;
                }
            }

            return true;
        });
    }

    private static bool Holds(ClassSession session, Instant now)
    {
        var booked = session.Bookings.Count(b => b.IsActive);
        var offers = session.Waitlist.Where(e => e.IsOffered).ToList();

        return booked + offers.Count <= session.Capacity
            && offers.All(e => e.OfferExpiresAt <= session.CancelUntil && session.OpenOfferOf(e.AccountId, now) is not null)
            && (!session.IsCancelled || (booked == 0 && session.WaitlistCount == 0))
            && session.Bookings.Where(b => b.IsActive && b.ClassMovedAt is not null)
                .All(b => session.CancelUntilFor(b.AccountId) == session.StartsAt);
    }
}
