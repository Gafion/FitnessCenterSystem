using CsCheck;
using FitnessCenter.Modules.Booking.Domain;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Tests;

/// <summary>
/// Property tests (D-036) for waitlist offers (D-051, D-067). Over any sequence of booking,
/// cancelling, joining, leaving, accepting, declining and time passing, with the offers brought
/// up to date after every step as the booking handler does, a spot is never offered to two
/// people at once: open offers and bookings together never exceed the capacity, and no member
/// holds two offers or both an offer and a booking.
/// </summary>
[TestClass]
public sealed class WaitlistOfferProperties
{
    private static readonly Instant Start = Instant.FromUtc(2027, 1, 12, 17, 0);

    private enum Action
    {
        Book,
        Cancel,
        Join,
        Leave,
        Accept,
        Decline,
        Wait,
    }

    private static readonly Guid[] Accounts =
        Enumerable.Range(1, 6).Select(n => new Guid(n, 0, 0, new byte[8])).ToArray();

    /// <summary>
    /// Steps at increasing times from 2 days before the class, up to 3 hours apart, so offers
    /// expire, and some scenarios pass the cancel deadline and the start.
    /// </summary>
    private static readonly Gen<(int Capacity, (Action Action, int Who, int Minutes)[] Steps)> Scenario =
        Gen.Select(
            Gen.Int[1, 3],
            Gen.Select(Gen.Enum<Action>(), Gen.Int[0, Accounts.Length - 1], Gen.Int[0, 180]).Array[0, 60]);

    [TestMethod]
    public void A_spot_is_never_offered_to_two_people_at_once()
    {
        Scenario.Sample((capacity, steps) =>
        {
            var session = Session(capacity);
            var now = Start - Duration.FromDays(2);

            foreach (var (action, who, minutes) in steps)
            {
                now += Duration.FromMinutes(minutes);
                var account = Accounts[who];
                session.UpdateOffers(now);
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
                    case Action.Leave:
                        session.LeaveWaitlist(account, now);
                        break;
                    case Action.Accept:
                        session.AcceptOffer(account, now);
                        break;
                    case Action.Decline:
                        session.DeclineOffer(account, now);
                        break;
                    case Action.Wait:
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

    /// <summary>An offer always ends within 2 hours of being made, and never after the cancel deadline.</summary>
    [TestMethod]
    public void Every_offer_ends_within_2_hours_and_by_the_cancel_deadline()
    {
        Scenario.Sample((capacity, steps) =>
        {
            var session = Session(capacity);
            var now = Start - Duration.FromDays(2);

            foreach (var (action, who, minutes) in steps)
            {
                now += Duration.FromMinutes(minutes);
                var account = Accounts[who];
                switch (action)
                {
                    case Action.Book or Action.Accept:
                        session.Book(account, now);
                        break;
                    case Action.Cancel:
                        session.Cancel(account, now);
                        break;
                    case Action.Join or Action.Wait:
                        session.JoinWaitlist(account, now);
                        break;
                    case Action.Leave or Action.Decline:
                        session.DeclineOffer(account, now);
                        break;
                }

                var offered = session.UpdateOffers(now);
                if (offered.Any(e => now >= session.CancelUntil
                    || e.OfferedAt != now
                    || e.OfferExpiresAt > now + Duration.FromHours(2)
                    || e.OfferExpiresAt > session.CancelUntil
                    || e.OfferExpiresAt <= now))
                {
                    return false;
                }
            }

            return true;
        });
    }

    private static bool Holds(ClassSession session, Instant now)
    {
        var booked = session.Bookings.Where(b => b.IsActive).Select(b => b.AccountId).ToList();
        var offered = session.Waitlist.Where(e => e.IsOffered).Select(e => e.AccountId).ToList();
        var queued = session.Waitlist.Where(e => e.IsQueued).Select(e => e.AccountId).ToList();

        return booked.Count + offered.Count <= session.Capacity
            && offered.Distinct().Count() == offered.Count
            && queued.Distinct().Count() == queued.Count
            && !offered.Any(booked.Contains)
            && session.SpotsLeft >= 0
            // After UpdateOffers, every held spot can still be accepted.
            && offered.All(account => session.OpenOfferOf(account, now) is not null);
    }

    private static ClassSession Session(int capacity) =>
        ClassSession.Schedule(ClassType.Spinning, "Test Instructor", Start, Duration.FromMinutes(50), capacity);
}
