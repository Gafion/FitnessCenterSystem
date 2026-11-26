using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Domain;

internal enum BookOutcome
{
    Booked,
    AlreadyBooked,
    Full,
    Started,
    NotOpenYet,

    /// <summary>
    /// The class starts after the last day of the member's access: their cancellation takes
    /// effect before it (D-048, D-055 option A).
    /// </summary>
    AfterMembershipEnds,

    /// <summary>The center cancelled the class (D-068).</summary>
    ClassCancelled,
}

internal enum JoinWaitlistOutcome
{
    Joined,
    AlreadyWaitlisted,
    AlreadyBooked,

    /// <summary>The class has a free spot, so the member books it instead.</summary>
    NotFull,
    Started,
    NotOpenYet,

    /// <inheritdoc cref="BookOutcome.AfterMembershipEnds"/>
    AfterMembershipEnds,

    /// <inheritdoc cref="BookOutcome.ClassCancelled"/>
    ClassCancelled,
}

internal enum LeaveWaitlistOutcome
{
    Left,
    NotWaitlisted,
}

internal enum AcceptOfferOutcome
{
    Booked,

    /// <summary>The account holds no offer for this class.</summary>
    NoOffer,

    /// <summary>The offer ran out before the member accepted it (D-067).</summary>
    Expired,

    /// <inheritdoc cref="BookOutcome.AfterMembershipEnds"/>
    AfterMembershipEnds,
}

internal enum DeclineOfferOutcome
{
    Declined,
    NoOffer,
}

internal enum CancelOutcome
{
    Cancelled,
    NotBooked,
    CancellationClosed,
}

internal enum ClassSessionStatus
{
    Scheduled,

    /// <summary>The center cancelled the class: its bookings are cancelled and its waitlist cleared (D-068).</summary>
    Cancelled,
}

/// <summary>What happened when staff changed a class (D-068).</summary>
internal enum ChangeClassOutcome
{
    Changed,

    /// <summary>A class that has started can't be changed.</summary>
    Started,

    /// <summary>A cancelled class can't be changed.</summary>
    ClassCancelled,

    /// <summary>The new start time has already passed.</summary>
    StartsInPast,

    /// <summary>The new capacity is below the booked spots plus the spots held by open offers.</summary>
    CapacityBelowBookings,
}

/// <param name="MovedFrom">The old start when the class moved to a new time, else null.</param>
/// <param name="BookedAccounts">The accounts with an active booking, who are told about a move.</param>
internal sealed record ClassChange(ChangeClassOutcome Outcome, Instant? MovedFrom, IReadOnlyList<Guid> BookedAccounts)
{
    public static ClassChange Refused(ChangeClassOutcome outcome) => new(outcome, null, []);
}

internal enum CancelClassOutcome
{
    Cancelled,
    Started,
    AlreadyCancelled,
}

/// <param name="CancelledBookings">The accounts whose booking the cancellation cancelled, who are emailed.</param>
internal sealed record ClassCancellation(CancelClassOutcome Outcome, IReadOnlyList<Guid> CancelledBookings);

/// <summary>
/// One scheduled class, e.g. Spinning on Tuesday at 18:00, with its bookings and its waitlist.
/// All booking rules (D-039) and waitlist rules (D-051) live here, so they can be tested
/// without a database.
/// </summary>
internal sealed class ClassSession
{
    private readonly List<ClassBooking> _bookings = [];

    private readonly List<WaitlistEntry> _waitlist = [];

    private ClassSession()
    {
    }

    public ClassSessionId Id { get; private set; }

    public ClassTypeId ClassTypeId { get; private set; }

    /// <summary>
    /// The Identity account of the instructor who teaches the class (D-060). Booking shows the
    /// name through Identity's contract, never by reading Identity's tables (rule 3).
    /// </summary>
    public Guid InstructorAccountId { get; private set; }

    public Instant StartsAt { get; private set; }

    public Instant EndsAt { get; private set; }

    public int Capacity { get; private set; }

    public ClassSessionStatus Status { get; private set; }

    /// <summary>When the center cancelled the class (D-068), else null.</summary>
    public Instant? CancelledAt { get; private set; }

    public bool IsCancelled => Status == ClassSessionStatus.Cancelled;

    /// <summary>The spots taken by active bookings.</summary>
    public int BookedCount => _bookings.Count(booking => booking.IsActive);

    /// <summary>The accounts waiting or holding an offer (D-051).</summary>
    public int WaitlistCount => _waitlist.Count(entry => entry.IsQueued);

    public IReadOnlyList<ClassBooking> Bookings => _bookings;

    /// <summary>Every waitlist entry, including the ones that left the queue.</summary>
    public IReadOnlyList<WaitlistEntry> Waitlist => _waitlist;

    /// <summary>Free spots: an open offer holds its spot for the member it was offered to (D-051).</summary>
    public int SpotsLeft => Capacity - BookedCount - HeldSpots;

    private int HeldSpots => _waitlist.Count(entry => entry.IsOffered);

    /// <summary>The last moment a member can cancel (D-039).</summary>
    public Instant CancelUntil => StartsAt - BookingPolicy.CancellationCutoff;

    public Duration Length => EndsAt - StartsAt;

    public static ClassSession Schedule(
        ClassType type, Guid instructorAccountId, Instant startsAt, Duration length, int capacity) =>
        Schedule(type.Id, instructorAccountId, startsAt, length, capacity);

    public static ClassSession Schedule(
        ClassTypeId typeId, Guid instructorAccountId, Instant startsAt, Duration length, int capacity)
    {
        CheckDetails(instructorAccountId, length, capacity);
        return new ClassSession
        {
            Id = ClassSessionId.New(),
            ClassTypeId = typeId,
            InstructorAccountId = instructorAccountId,
            StartsAt = startsAt,
            EndsAt = startsAt + length,
            Capacity = capacity,
            Status = ClassSessionStatus.Scheduled,
        };
    }

    /// <summary>
    /// Staff change the class's type, instructor, time, length or capacity (D-068). A new start
    /// time moves the class: its bookings and its waitlist stay, and every booked member may
    /// then cancel until the class starts, even after the 12-hour deadline (D-039). An open
    /// offer never runs past the new cancel deadline (D-067). Capacity can't go below the
    /// booked spots plus the spots held by open offers. Call <see cref="UpdateOffers"/> first,
    /// so offers that ran out don't count, and after, so new spots are offered to the queue.
    /// </summary>
    public ClassChange Change(
        ClassTypeId typeId, Guid instructorAccountId, Instant startsAt, Duration length, int capacity, Instant now)
    {
        CheckDetails(instructorAccountId, length, capacity);
        if (IsCancelled)
        {
            return ClassChange.Refused(ChangeClassOutcome.ClassCancelled);
        }

        if (now >= StartsAt)
        {
            return ClassChange.Refused(ChangeClassOutcome.Started);
        }

        var moves = startsAt != StartsAt;
        if (moves && startsAt <= now)
        {
            return ClassChange.Refused(ChangeClassOutcome.StartsInPast);
        }

        if (capacity < BookedCount + HeldSpots)
        {
            return ClassChange.Refused(ChangeClassOutcome.CapacityBelowBookings);
        }

        var movedFrom = moves ? StartsAt : (Instant?)null;
        ClassTypeId = typeId;
        InstructorAccountId = instructorAccountId;
        StartsAt = startsAt;
        EndsAt = startsAt + length;
        Capacity = capacity;

        var booked = _bookings.Where(booking => booking.IsActive).ToList();
        if (moves)
        {
            foreach (var booking in booked)
            {
                booking.ClassMoved(now);
            }

            foreach (var entry in _waitlist)
            {
                entry.EndOfferBy(CancelUntil);
            }
        }

        return new ClassChange(ChangeClassOutcome.Changed, movedFrom, booked.Select(booking => booking.AccountId).ToList());
    }

    /// <summary>
    /// The center cancels the class (D-068): every booking is cancelled and the waitlist is
    /// cleared, including any spot held by an offer. The 12-hour deadline (D-039) doesn't
    /// apply; a class that has started can't be cancelled.
    /// </summary>
    public ClassCancellation CancelClass(Instant now)
    {
        if (IsCancelled)
        {
            return new ClassCancellation(CancelClassOutcome.AlreadyCancelled, []);
        }

        if (now >= StartsAt)
        {
            return new ClassCancellation(CancelClassOutcome.Started, []);
        }

        var booked = _bookings.Where(booking => booking.IsActive).ToList();
        foreach (var booking in booked)
        {
            booking.CancelBecauseClassCancelled(now);
        }

        foreach (var entry in _waitlist.Where(entry => entry.IsQueued))
        {
            entry.RemoveBecauseClassCancelled(now);
        }

        Status = ClassSessionStatus.Cancelled;
        CancelledAt = now;
        return new ClassCancellation(CancelClassOutcome.Cancelled, booked.Select(booking => booking.AccountId).ToList());
    }

    /// <summary>
    /// The last moment the account can cancel its booking: 12 hours before the class (D-039),
    /// or the start itself when the class moved after the account booked it (D-068).
    /// </summary>
    public Instant CancelUntilFor(Guid accountId) =>
        ActiveBookingOf(accountId) is { ClassMovedAt: not null } ? StartsAt : CancelUntil;

    private static void CheckDetails(Guid instructorAccountId, Duration length, int capacity)
    {
        if (instructorAccountId == Guid.Empty)
        {
            throw new ArgumentException("A class needs an instructor account (D-060).", nameof(instructorAccountId));
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A class needs at least one spot.");
        }

        if (length <= Duration.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "A class must have a length.");
        }
    }

    public bool IsBookedBy(Guid accountId) => ActiveBookingOf(accountId) is not null;

    /// <summary>
    /// The members waiting for a spot, first come, first served (D-051). A member who holds an
    /// offer has left the queue's line: their spot is set aside.
    /// </summary>
    public IReadOnlyList<WaitlistEntry> Queue =>
        _waitlist
            .Where(entry => entry.IsWaiting)
            .OrderBy(entry => entry.Number)
            .ToList();

    /// <summary>The account's place in the queue, counting from 1, or null when it isn't waiting.</summary>
    public int? WaitlistPlaceOf(Guid accountId)
    {
        var index = Queue.ToList().FindIndex(entry => entry.AccountId == accountId);
        return index < 0 ? null : index + 1;
    }

    /// <param name="lastDayOfAccess">
    /// The member's last day of access (Copenhagen) when they have cancelled, else null (D-048).
    /// </param>
    public BookOutcome Book(Guid accountId, Instant now, LocalDate? lastDayOfAccess = null)
    {
        // Booking a class whose spot is held for this member accepts the offer.
        if (OpenOfferOf(accountId, now) is not null)
        {
            return AcceptOffer(accountId, now, lastDayOfAccess) switch
            {
                AcceptOfferOutcome.Booked => BookOutcome.Booked,
                AcceptOfferOutcome.AfterMembershipEnds => BookOutcome.AfterMembershipEnds,
                var other => throw new InvalidOperationException($"An open offer can't end in {other}."),
            };
        }

        if (IsCancelled)
        {
            return BookOutcome.ClassCancelled;
        }

        if (now >= StartsAt)
        {
            return BookOutcome.Started;
        }

        if (StartsAt - now > BookingPolicy.BookingWindow)
        {
            return BookOutcome.NotOpenYet;
        }

        if (IsBookedBy(accountId))
        {
            return BookOutcome.AlreadyBooked;
        }

        if (StartsAfter(lastDayOfAccess))
        {
            return BookOutcome.AfterMembershipEnds;
        }

        if (SpotsLeft <= 0)
        {
            return BookOutcome.Full;
        }

        // A member is never both booked and waitlisted: booking takes them out of the queue.
        QueuedEntryOf(accountId)?.EndBecauseBooked(now);
        _bookings.Add(ClassBooking.Create(Id, accountId, now));
        return BookOutcome.Booked;
    }

    /// <summary>
    /// Puts the account at the back of the queue of a full class (D-051). The same booking
    /// window (D-039) and membership rule (D-055) apply as for booking.
    /// </summary>
    /// <param name="lastDayOfAccess">
    /// The member's last day of access (Copenhagen) when they have cancelled, else null (D-048).
    /// </param>
    public JoinWaitlistOutcome JoinWaitlist(Guid accountId, Instant now, LocalDate? lastDayOfAccess = null)
    {
        if (IsCancelled)
        {
            return JoinWaitlistOutcome.ClassCancelled;
        }

        if (now >= StartsAt)
        {
            return JoinWaitlistOutcome.Started;
        }

        if (StartsAt - now > BookingPolicy.BookingWindow)
        {
            return JoinWaitlistOutcome.NotOpenYet;
        }

        if (IsBookedBy(accountId))
        {
            return JoinWaitlistOutcome.AlreadyBooked;
        }

        if (QueuedEntryOf(accountId) is not null)
        {
            return JoinWaitlistOutcome.AlreadyWaitlisted;
        }

        if (StartsAfter(lastDayOfAccess))
        {
            return JoinWaitlistOutcome.AfterMembershipEnds;
        }

        if (SpotsLeft > 0)
        {
            return JoinWaitlistOutcome.NotFull;
        }

        var number = _waitlist.Count == 0 ? 1 : _waitlist.Max(entry => entry.Number) + 1;
        _waitlist.Add(WaitlistEntry.Create(Id, accountId, number, now));
        return JoinWaitlistOutcome.Joined;
    }

    /// <summary>
    /// Takes the account out of the queue. Leaving is always allowed; there are no penalties
    /// (D-051). Leaving while holding an offer gives the spot up, like declining it.
    /// </summary>
    public LeaveWaitlistOutcome LeaveWaitlist(Guid accountId, Instant now)
    {
        if (QueuedEntryOf(accountId) is not { } entry)
        {
            return LeaveWaitlistOutcome.NotWaitlisted;
        }

        entry.Leave(now);
        return LeaveWaitlistOutcome.Left;
    }

    /// <summary>
    /// Whether the class starts on a Copenhagen day after <paramref name="lastDayOfAccess"/>
    /// (D-055 option A). A class on the last day itself is still within the membership.
    /// </summary>
    public bool StartsAfter(LocalDate? lastDayOfAccess) =>
        lastDayOfAccess is { } lastDay && StartsAt.InZone(CenterTime.Zone).Date > lastDay;

    /// <summary>
    /// The member cancels their booking, until <see cref="CancelUntilFor"/>: 12 hours before
    /// the class (D-039), or until it starts when the class moved after they booked (D-068).
    /// </summary>
    public CancelOutcome Cancel(Guid accountId, Instant now)
    {
        var booking = ActiveBookingOf(accountId);
        if (booking is null)
        {
            return CancelOutcome.NotBooked;
        }

        var closed = booking.ClassMovedAt is null ? now > CancelUntil : now >= StartsAt;
        if (closed)
        {
            return CancelOutcome.CancellationClosed;
        }

        booking.Cancel(now);
        return CancelOutcome.Cancelled;
    }

    /// <summary>
    /// Cancels the account's booking because their membership ends before this class: the
    /// class starts on a Copenhagen day after <paramref name="lastDayOfAccess"/> (D-055
    /// amendment, FIT-97). The 12-hour cut-off (D-039) doesn't apply; it limits what members
    /// do themselves, and here the system frees the spot.
    /// </summary>
    /// <returns>True when a booking was cancelled.</returns>
    public bool CancelBecauseMembershipEnds(Guid accountId, LocalDate lastDayOfAccess, Instant now)
    {
        if (!StartsAfter(lastDayOfAccess) || ActiveBookingOf(accountId) is not { } booking)
        {
            return false;
        }

        booking.Cancel(now);
        return true;
    }

    /// <summary>
    /// Brings the offers up to date at <paramref name="now"/> (D-051, D-067). First, offers
    /// that ran out expire. Then every free spot is offered to the next person in the queue,
    /// until the spots or the queue run out. An offer ends 2 hours after it is made, or at the
    /// cancel deadline if that comes first. Once the cancel deadline has passed, no new offer
    /// is made: a spot that frees up after it is open to anyone who books first.
    /// </summary>
    /// <returns>The entries that got a new offer, so the members can be told.</returns>
    public IReadOnlyList<WaitlistEntry> UpdateOffers(Instant now)
    {
        foreach (var entry in _waitlist.Where(entry => entry.IsOffered && !entry.IsOfferOpenAt(now)))
        {
            entry.Expire();
        }

        var offered = new List<WaitlistEntry>();
        if (IsCancelled || now >= CancelUntil)
        {
            return offered;
        }

        var expiresAt = Instant.Min(now + BookingPolicy.OfferLength, CancelUntil);
        foreach (var next in Queue.Take(Math.Max(SpotsLeft, 0)))
        {
            next.Offer(now, expiresAt);
            offered.Add(next);
        }

        return offered;
    }

    /// <summary>The account's offer for this class while it can still be accepted, else null.</summary>
    public WaitlistEntry? OpenOfferOf(Guid accountId, Instant now) =>
        QueuedEntryOf(accountId) is { } entry && entry.IsOfferOpenAt(now) ? entry : null;

    /// <summary>Books the spot held for the account (D-051), while the offer is open.</summary>
    /// <param name="lastDayOfAccess">
    /// The member's last day of access (Copenhagen) when they have cancelled, else null (D-048).
    /// </param>
    public AcceptOfferOutcome AcceptOffer(Guid accountId, Instant now, LocalDate? lastDayOfAccess = null)
    {
        var latest = LatestEntryOf(accountId);
        if (latest is { IsOffered: true } && !latest.IsOfferOpenAt(now))
        {
            latest.Expire();
        }

        if (latest is { Status: WaitlistEntryStatus.Expired })
        {
            return AcceptOfferOutcome.Expired;
        }

        if (latest is not { IsOffered: true } offer)
        {
            return AcceptOfferOutcome.NoOffer;
        }

        if (StartsAfter(lastDayOfAccess))
        {
            return AcceptOfferOutcome.AfterMembershipEnds;
        }

        offer.EndBecauseBooked(now);
        _bookings.Add(ClassBooking.Create(Id, accountId, now));
        return AcceptOfferOutcome.Booked;
    }

    /// <summary>Gives the held spot up; <see cref="UpdateOffers"/> then offers it to the next person.</summary>
    public DeclineOfferOutcome DeclineOffer(Guid accountId, Instant now)
    {
        if (OpenOfferOf(accountId, now) is not { } offer)
        {
            return DeclineOfferOutcome.NoOffer;
        }

        offer.Decline(now);
        return DeclineOfferOutcome.Declined;
    }

    /// <summary>
    /// Takes the account off this class's waitlist because their membership ends before the
    /// class (D-055 amendment, FIT-80), as <see cref="CancelBecauseMembershipEnds"/> does for
    /// bookings. An offer the account held is given up.
    /// </summary>
    /// <returns>True when an entry was removed.</returns>
    public bool RemoveFromWaitlistBecauseMembershipEnds(Guid accountId, LocalDate lastDayOfAccess, Instant now)
    {
        if (!StartsAfter(lastDayOfAccess) || QueuedEntryOf(accountId) is not { } entry)
        {
            return false;
        }

        entry.RemoveBecauseMembershipEnds(now);
        return true;
    }

    /// <summary>The account's entry while it waits or holds an offer.</summary>
    private WaitlistEntry? QueuedEntryOf(Guid accountId) =>
        _waitlist.SingleOrDefault(entry => entry.IsQueued && entry.AccountId == accountId);

    private WaitlistEntry? LatestEntryOf(Guid accountId) =>
        _waitlist.Where(entry => entry.AccountId == accountId).MaxBy(entry => entry.Number);

    private ClassBooking? ActiveBookingOf(Guid accountId) =>
        _bookings.SingleOrDefault(booking => booking.IsActive && booking.AccountId == accountId);
}
