using CsCheck;
using FitnessCenter.Modules.Booking.Domain;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Tests;

/// <summary>
/// Property tests (D-036) for the waitlist (D-051): over any sequence of booking, cancelling,
/// joining and leaving by a few members, the queue stays first come, first served and nobody
/// is both booked and waiting.
/// </summary>
[TestClass]
public sealed class WaitlistProperties
{
    private static readonly Instant Start = Instant.FromUtc(2027, 1, 12, 17, 0);

    private enum Action
    {
        Book,
        Cancel,
        Join,
        Leave,
    }

    private static readonly Guid[] Accounts =
        Enumerable.Range(1, 6).Select(n => new Guid(n, 0, 0, new byte[8])).ToArray();

    /// <summary>Steps at increasing times within the booking window, before the cancel cut-off.</summary>
    private static readonly Gen<(int Capacity, (Action Action, int Who, int Minutes)[] Steps)> Scenario =
        Gen.Select(
            Gen.Int[1, 3],
            Gen.Select(Gen.Enum<Action>(), Gen.Int[0, Accounts.Length - 1], Gen.Int[0, 5]).Array[0, 60]);

    [TestMethod]
    public void Nobody_is_both_booked_and_waiting_and_the_queue_is_first_come_first_served()
    {
        Scenario.Sample((capacity, steps) =>
        {
            var session = Session(capacity);
            var now = Start - Duration.FromDays(10);
            // The order members joined in, as a model of the queue.
            var expectedQueue = new List<Guid>();

            foreach (var (action, who, minutes) in steps)
            {
                now += Duration.FromMinutes(minutes);
                var account = Accounts[who];
                switch (action)
                {
                    case Action.Book:
                        if (session.Book(account, now) == BookOutcome.Booked)
                        {
                            expectedQueue.Remove(account);
                        }

                        break;
                    case Action.Cancel:
                        session.Cancel(account, now);
                        break;
                    case Action.Join:
                        if (session.JoinWaitlist(account, now) == JoinWaitlistOutcome.Joined)
                        {
                            expectedQueue.Add(account);
                        }

                        break;
                    case Action.Leave:
                        if (session.LeaveWaitlist(account, now) == LeaveWaitlistOutcome.Left)
                        {
                            expectedQueue.Remove(account);
                        }

                        break;
                }
            }

            var queue = session.Queue.Select(e => e.AccountId).ToList();
            var places = queue.Select(session.WaitlistPlaceOf).ToList();
            return queue.SequenceEqual(expectedQueue)
                && queue.Distinct().Count() == queue.Count
                && !queue.Any(session.IsBookedBy)
                && places.SequenceEqual(Enumerable.Range(1, queue.Count).Select(n => (int?)n));
        });
    }

    /// <summary>Joining succeeds exactly when the class is full and the member is neither booked nor waiting.</summary>
    [TestMethod]
    public void Joining_succeeds_exactly_for_a_full_class_one_is_not_in()
    {
        Scenario.Sample((capacity, steps) =>
        {
            var session = Session(capacity);
            var now = Start - Duration.FromDays(10);
            foreach (var (action, who, minutes) in steps)
            {
                now += Duration.FromMinutes(minutes);
                var account = Accounts[who];
                var full = session.SpotsLeft == 0;
                var booked = session.IsBookedBy(account);
                var waiting = session.WaitlistPlaceOf(account) is not null;

                if (action is Action.Join or Action.Leave)
                {
                    var joined = session.JoinWaitlist(account, now) == JoinWaitlistOutcome.Joined;
                    if (joined != (full && !booked && !waiting))
                    {
                        return false;
                    }
                }
                else if (action == Action.Book)
                {
                    session.Book(account, now);
                }
                else
                {
                    session.Cancel(account, now);
                }
            }

            return true;
        });
    }

    private static ClassSession Session(int capacity) =>
        ClassSession.Schedule(ClassType.Spinning, TestInstructor.Id, Start, Duration.FromMinutes(50), capacity);
}
