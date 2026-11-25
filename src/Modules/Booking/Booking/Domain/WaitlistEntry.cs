using NodaTime;

namespace FitnessCenter.Modules.Booking.Domain;

internal enum WaitlistEntryStatus
{
    /// <summary>In the queue for a spot.</summary>
    Waiting,

    /// <summary>The member left the queue themselves.</summary>
    Left,

    /// <summary>The member booked a spot in the class, which took them out of the queue.</summary>
    Booked,
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

    public bool IsWaiting => Status == WaitlistEntryStatus.Waiting;

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

    private void End(WaitlistEntryStatus status, Instant now)
    {
        Status = status;
        LeftAt = now;
    }
}
