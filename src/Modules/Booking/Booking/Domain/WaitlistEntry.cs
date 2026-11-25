using NodaTime;

namespace FitnessCenter.Modules.Booking.Domain;

internal enum WaitlistEntryStatus
{
    /// <summary>In the queue for a spot.</summary>
    Waiting,

    /// <summary>The member left the queue themselves.</summary>
    Left,

    /// <summary>
    /// The member booked a spot in the class, which took them out of the queue. Accepting an
    /// offer ends here too.
    /// </summary>
    Booked,

    /// <summary>A freed spot is held for the member until the offer expires (D-051, D-067).</summary>
    Offered,

    /// <summary>The member said no to the offer; the spot went to the next person.</summary>
    Declined,

    /// <summary>The offer ran out unanswered; the spot went to the next person (D-067).</summary>
    Expired,

    /// <summary>
    /// The member's membership ends before the class, so the system took them off the queue
    /// (D-055 amendment, FIT-80), as it cancels their bookings.
    /// </summary>
    RemovedMembershipEnds,

    /// <summary>The center cancelled the class, which clears its waitlist (D-068).</summary>
    ClassCancelled,
}

/// <summary>
/// One account's place in the waitlist of a full class (D-051). The queue is first come, first
/// served: ordered by <see cref="Number"/>, like a ticket from a queue machine. An entry that
/// left the queue stays as history, like a cancelled booking.
/// </summary>
internal sealed class WaitlistEntry
{
    private WaitlistEntry()
    {
    }

    public WaitlistEntryId Id { get; private set; }

    public ClassSessionId ClassSessionId { get; private set; }

    public Guid AccountId { get; private set; }

    /// <summary>
    /// The entry's ticket number within its class: one more than the class's highest so far.
    /// Join times alone can tie, and time-ordered ids aren't ordered within a millisecond.
    /// </summary>
    public int Number { get; private set; }

    public WaitlistEntryStatus Status { get; private set; }

    public Instant JoinedAt { get; private set; }

    public Instant? LeftAt { get; private set; }

    /// <summary>When the open offer was made (D-067), or null when there has been none.</summary>
    public Instant? OfferedAt { get; private set; }

    /// <summary>When the offer ends unanswered (D-067). Null when there has been no offer.</summary>
    public Instant? OfferExpiresAt { get; private set; }

    /// <summary>In the queue, still waiting for an offer.</summary>
    public bool IsWaiting => Status == WaitlistEntryStatus.Waiting;

    /// <summary>Holds a spot: an offer was made and not yet answered or expired by the class.</summary>
    public bool IsOffered => Status == WaitlistEntryStatus.Offered;

    /// <summary>Waiting or holding an offer: the account is still on this class's waitlist.</summary>
    public bool IsQueued => IsWaiting || IsOffered;

    /// <summary>Whether the offer can still be accepted at <paramref name="now"/>.</summary>
    public bool IsOfferOpenAt(Instant now) => IsOffered && now < OfferExpiresAt;

    internal static WaitlistEntry Create(ClassSessionId sessionId, Guid accountId, int number, Instant now) => new()
    {
        Id = WaitlistEntryId.New(),
        ClassSessionId = sessionId,
        AccountId = accountId,
        Number = number,
        Status = WaitlistEntryStatus.Waiting,
        JoinedAt = now,
    };

    internal void Leave(Instant now) => End(WaitlistEntryStatus.Left, now);

    internal void EndBecauseBooked(Instant now) => End(WaitlistEntryStatus.Booked, now);

    internal void Offer(Instant now, Instant expiresAt)
    {
        Status = WaitlistEntryStatus.Offered;
        OfferedAt = now;
        OfferExpiresAt = expiresAt;
    }

    internal void Decline(Instant now) => End(WaitlistEntryStatus.Declined, now);

    /// <summary>The offer ran out; it ended at its expiry, whenever the system noticed.</summary>
    internal void Expire() => End(WaitlistEntryStatus.Expired, OfferExpiresAt!.Value);

    internal void RemoveBecauseMembershipEnds(Instant now) => End(WaitlistEntryStatus.RemovedMembershipEnds, now);

    internal void RemoveBecauseClassCancelled(Instant now) => End(WaitlistEntryStatus.ClassCancelled, now);

    /// <summary>
    /// Ends an open offer no later than <paramref name="latest"/>, when the class moved and its
    /// cancel deadline came closer (D-067, D-068). Never before the offer was made.
    /// </summary>
    internal void EndOfferBy(Instant latest)
    {
        if (IsOffered && OfferExpiresAt > latest)
        {
            OfferExpiresAt = Instant.Max(latest, OfferedAt!.Value);
        }
    }

    private void End(WaitlistEntryStatus status, Instant now)
    {
        Status = status;
        LeftAt = now;
    }
}
