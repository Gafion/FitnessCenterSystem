using FitnessCenter.Modules.Booking.Domain;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Tests;

/// <summary>
/// Examples of waitlist offers (D-051, D-067): a freed spot is held for the next person for
/// 2 hours, never past the cancel deadline. Time comes from a fake clock that the tests move.
/// <see cref="WaitlistOfferProperties"/> checks the rules over generated scenarios.
/// </summary>
[TestClass]
public sealed class WaitlistOfferTests
{
    private static readonly Instant Start = Instant.FromUtc(2027, 1, 12, 17, 0);
    private static readonly Guid Ann = Guid.Parse("0199a3c0-0000-7000-8000-00000000b001");
    private static readonly Guid Ben = Guid.Parse("0199a3c0-0000-7000-8000-00000000b002");
    private static readonly Guid Cleo = Guid.Parse("0199a3c0-0000-7000-8000-00000000b003");
    private static readonly Guid Dan = Guid.Parse("0199a3c0-0000-7000-8000-00000000b004");
    private static readonly Guid Eve = Guid.Parse("0199a3c0-0000-7000-8000-00000000b005");

    private readonly FakeClock _clock = new(Start - Duration.FromDays(1));

    [TestMethod]
    public void A_cancelled_spot_is_offered_to_the_first_in_the_queue_for_2_hours()
    {
        var session = FullSessionWithQueue(Ben, Cleo);

        session.Cancel(Ann, _clock.Now);
        var offered = session.UpdateOffers(_clock.Now);

        Assert.AreEqual(Ben, offered.Single().AccountId);
        Assert.AreEqual(_clock.Now + Duration.FromHours(2), offered.Single().OfferExpiresAt);
        Assert.AreEqual(_clock.Now + Duration.FromHours(2), session.OpenOfferOf(Ben, _clock.Now)!.OfferExpiresAt);
        Assert.IsNull(session.OpenOfferOf(Cleo, _clock.Now));
        // The held spot is not free for anyone else.
        Assert.AreEqual(0, session.SpotsLeft);
        Assert.AreEqual(BookOutcome.Full, session.Book(Eve, _clock.Now));
    }

    [TestMethod]
    public void Accepting_in_time_books_the_spot()
    {
        var session = FullSessionWithQueue(Ben, Cleo);
        session.Cancel(Ann, _clock.Now);
        session.UpdateOffers(_clock.Now);

        _clock.Advance(Duration.FromHours(2) - Duration.FromSeconds(1));

        Assert.AreEqual(AcceptOfferOutcome.Booked, session.AcceptOffer(Ben, _clock.Now));
        Assert.IsTrue(session.IsBookedBy(Ben));
        Assert.AreEqual(WaitlistEntryStatus.Booked, session.Waitlist.Single(e => e.AccountId == Ben).Status);
        Assert.AreEqual(1, session.WaitlistPlaceOf(Cleo));
        Assert.AreEqual(0, session.SpotsLeft);
        Assert.IsEmpty(session.UpdateOffers(_clock.Now));
    }

    [TestMethod]
    public void Booking_a_class_whose_spot_is_held_for_you_accepts_the_offer()
    {
        var session = FullSessionWithQueue(Ben);
        session.Cancel(Ann, _clock.Now);
        session.UpdateOffers(_clock.Now);

        Assert.AreEqual(BookOutcome.Booked, session.Book(Ben, _clock.Now + Duration.FromMinutes(5)));
        Assert.AreEqual(WaitlistEntryStatus.Booked, session.Waitlist.Single(e => e.AccountId == Ben).Status);
    }

    [TestMethod]
    public void An_offer_that_runs_out_expires_and_the_spot_goes_to_the_next_person()
    {
        var session = FullSessionWithQueue(Ben, Cleo);
        session.Cancel(Ann, _clock.Now);
        session.UpdateOffers(_clock.Now);
        var expiresAt = session.OpenOfferOf(Ben, _clock.Now)!.OfferExpiresAt!.Value;

        _clock.Advance(Duration.FromHours(2));
        var offered = session.UpdateOffers(_clock.Now);

        var ben = session.Waitlist.Single(e => e.AccountId == Ben);
        Assert.AreEqual(WaitlistEntryStatus.Expired, ben.Status);
        Assert.AreEqual(expiresAt, ben.LeftAt);
        Assert.AreEqual(Cleo, offered.Single().AccountId);
        Assert.AreEqual(AcceptOfferOutcome.Expired, session.AcceptOffer(Ben, _clock.Now));
        Assert.IsFalse(session.IsBookedBy(Ben));
    }

    [TestMethod]
    public void Accepting_after_the_expiry_fails_even_before_the_offers_are_updated()
    {
        var session = FullSessionWithQueue(Ben, Cleo);
        session.Cancel(Ann, _clock.Now);
        session.UpdateOffers(_clock.Now);

        _clock.Advance(Duration.FromHours(3));

        Assert.AreEqual(AcceptOfferOutcome.Expired, session.AcceptOffer(Ben, _clock.Now));
        Assert.IsFalse(session.IsBookedBy(Ben));
        Assert.AreEqual(WaitlistEntryStatus.Expired, session.Waitlist.Single(e => e.AccountId == Ben).Status);
    }

    [TestMethod]
    public void Declining_gives_the_spot_to_the_next_person()
    {
        var session = FullSessionWithQueue(Ben, Cleo, Dan);
        session.Cancel(Ann, _clock.Now);
        session.UpdateOffers(_clock.Now);

        _clock.Advance(Duration.FromMinutes(10));
        Assert.AreEqual(DeclineOfferOutcome.Declined, session.DeclineOffer(Ben, _clock.Now));
        var offered = session.UpdateOffers(_clock.Now);

        Assert.AreEqual(WaitlistEntryStatus.Declined, session.Waitlist.Single(e => e.AccountId == Ben).Status);
        Assert.AreEqual(Cleo, offered.Single().AccountId);
        Assert.AreEqual(1, session.WaitlistPlaceOf(Dan));
        Assert.AreEqual(AcceptOfferOutcome.NoOffer, session.AcceptOffer(Ben, _clock.Now));
    }

    [TestMethod]
    public void Declining_or_accepting_without_an_offer_does_nothing()
    {
        var session = FullSessionWithQueue(Ben);

        Assert.AreEqual(DeclineOfferOutcome.NoOffer, session.DeclineOffer(Ben, _clock.Now));
        Assert.AreEqual(AcceptOfferOutcome.NoOffer, session.AcceptOffer(Ben, _clock.Now));
        Assert.AreEqual(1, session.WaitlistPlaceOf(Ben));
    }

    [TestMethod]
    public void With_an_empty_queue_no_offer_is_made_and_the_spot_is_free_for_anyone()
    {
        var session = FullSessionWithQueue();

        session.Cancel(Ann, _clock.Now);

        Assert.IsEmpty(session.UpdateOffers(_clock.Now));
        Assert.AreEqual(1, session.SpotsLeft);
        Assert.AreEqual(BookOutcome.Booked, session.Book(Eve, _clock.Now));
    }

    [TestMethod]
    public void When_the_last_offer_expires_with_nobody_left_the_spot_becomes_free()
    {
        var session = FullSessionWithQueue(Ben);
        session.Cancel(Ann, _clock.Now);
        session.UpdateOffers(_clock.Now);

        _clock.Advance(Duration.FromHours(2));

        Assert.IsEmpty(session.UpdateOffers(_clock.Now));
        Assert.AreEqual(1, session.SpotsLeft);
        Assert.AreEqual(BookOutcome.Booked, session.Book(Eve, _clock.Now));
    }

    [TestMethod]
    public void Two_freed_spots_go_to_the_first_two_in_the_queue()
    {
        var session = Session(capacity: 2);
        session.Book(Ann, _clock.Now);
        session.Book(Eve, _clock.Now);
        session.JoinWaitlist(Ben, _clock.Now);
        session.JoinWaitlist(Cleo, _clock.Now);
        session.JoinWaitlist(Dan, _clock.Now);

        session.Cancel(Ann, _clock.Now);
        session.Cancel(Eve, _clock.Now);
        var offered = session.UpdateOffers(_clock.Now);

        CollectionAssert.AreEqual(new[] { Ben, Cleo }, offered.Select(e => e.AccountId).ToArray());
        Assert.AreEqual(1, session.WaitlistPlaceOf(Dan));
    }

    /// <summary>Less than 2 hours before the cancel deadline, the offer ends at the deadline (D-067).</summary>
    [TestMethod]
    public void An_offer_never_runs_past_the_cancel_deadline()
    {
        var session = FullSessionWithQueue(Ben);
        _clock.Set(session.CancelUntil - Duration.FromMinutes(30));

        session.Cancel(Ann, _clock.Now);
        var offered = session.UpdateOffers(_clock.Now);

        Assert.AreEqual(session.CancelUntil, offered.Single().OfferExpiresAt);
        Assert.AreEqual(session.StartsAt - Duration.FromHours(12), offered.Single().OfferExpiresAt);

        _clock.Set(session.CancelUntil);
        Assert.AreEqual(AcceptOfferOutcome.Expired, session.AcceptOffer(Ben, _clock.Now));
    }

    /// <summary>Exactly 2 hours before the deadline, the offer gets its full 2 hours.</summary>
    [TestMethod]
    public void An_offer_made_2_hours_before_the_cancel_deadline_lasts_until_it()
    {
        var session = FullSessionWithQueue(Ben);
        _clock.Set(session.CancelUntil - Duration.FromHours(2));

        session.Cancel(Ann, _clock.Now);

        Assert.AreEqual(session.CancelUntil, session.UpdateOffers(_clock.Now).Single().OfferExpiresAt);
    }

    /// <summary>
    /// Once the cancel deadline has passed, no offer is made: the spot is open to whoever books
    /// first (D-067). The system freed it here, as when a membership ends (D-055 amendment).
    /// </summary>
    [TestMethod]
    public void After_the_cancel_deadline_no_offer_is_made_and_anyone_can_book()
    {
        var session = FullSessionWithQueue(Ben);
        _clock.Set(session.CancelUntil + Duration.FromHours(1));

        Assert.IsTrue(session.CancelBecauseMembershipEnds(Ann, session.StartsAt.InUtc().Date.PlusDays(-1), _clock.Now));

        Assert.IsEmpty(session.UpdateOffers(_clock.Now));
        Assert.AreEqual(1, session.WaitlistPlaceOf(Ben));
        Assert.AreEqual(BookOutcome.Booked, session.Book(Eve, _clock.Now));
    }

    [TestMethod]
    public void Accepting_a_class_after_the_membership_ends_is_refused()
    {
        var session = FullSessionWithQueue(Ben);
        session.Cancel(Ann, _clock.Now);
        session.UpdateOffers(_clock.Now);
        var dayBeforeClass = Start.InUtc().Date.PlusDays(-1);

        Assert.AreEqual(AcceptOfferOutcome.AfterMembershipEnds, session.AcceptOffer(Ben, _clock.Now, dayBeforeClass));
        Assert.IsFalse(session.IsBookedBy(Ben));
    }

    [TestMethod]
    public void A_member_whose_membership_ends_before_the_class_is_taken_off_the_waitlist()
    {
        var session = FullSessionWithQueue(Ben, Cleo);
        var dayBeforeClass = Start.InUtc().Date.PlusDays(-1);

        Assert.IsTrue(session.RemoveFromWaitlistBecauseMembershipEnds(Ben, dayBeforeClass, _clock.Now));

        Assert.AreEqual(WaitlistEntryStatus.RemovedMembershipEnds, session.Waitlist.Single(e => e.AccountId == Ben).Status);
        Assert.IsNull(session.WaitlistPlaceOf(Ben));
        Assert.AreEqual(1, session.WaitlistPlaceOf(Cleo));
        Assert.IsFalse(session.RemoveFromWaitlistBecauseMembershipEnds(Ben, dayBeforeClass, _clock.Now));
    }

    [TestMethod]
    public void A_member_whose_membership_ends_on_the_class_day_stays_on_the_waitlist()
    {
        var session = FullSessionWithQueue(Ben);
        var classDay = Start.InZone(FitnessCenter.SharedKernel.CenterTime.Zone).Date;

        Assert.IsFalse(session.RemoveFromWaitlistBecauseMembershipEnds(Ben, classDay, _clock.Now));
        Assert.AreEqual(1, session.WaitlistPlaceOf(Ben));
    }

    [TestMethod]
    public void A_held_spot_goes_to_the_next_person_when_the_holders_membership_ends()
    {
        var session = FullSessionWithQueue(Ben, Cleo);
        session.Cancel(Ann, _clock.Now);
        session.UpdateOffers(_clock.Now);

        session.RemoveFromWaitlistBecauseMembershipEnds(Ben, Start.InUtc().Date.PlusDays(-1), _clock.Now);
        var offered = session.UpdateOffers(_clock.Now);

        Assert.AreEqual(Cleo, offered.Single().AccountId);
        Assert.IsNull(session.OpenOfferOf(Ben, _clock.Now));
    }

    /// <summary>A one-spot class that <see cref="Ann"/> has booked, with the given members queued in order.</summary>
    private ClassSession FullSessionWithQueue(params Guid[] queue)
    {
        var session = Session(capacity: 1);
        session.Book(Ann, _clock.Now);
        foreach (var account in queue)
        {
            Assert.AreEqual(JoinWaitlistOutcome.Joined, session.JoinWaitlist(account, _clock.Now));
        }

        return session;
    }

    private static ClassSession Session(int capacity) =>
        ClassSession.Schedule(ClassType.Spinning, TestInstructor.Id, Start, Duration.FromMinutes(50), capacity);

    /// <summary>A clock the test moves by hand.</summary>
    private sealed class FakeClock(Instant now) : IClock
    {
        public Instant Now { get; private set; } = now;

        public Instant GetCurrentInstant() => Now;

        public void Advance(Duration by) => Now += by;

        public void Set(Instant to) => Now = to;
    }
}
