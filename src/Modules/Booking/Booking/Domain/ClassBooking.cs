using NodaTime;

namespace FitnessCenter.Modules.Booking.Domain;

internal enum ClassBookingStatus
{
    Booked,
    Cancelled,

    /// <summary>The center cancelled the whole class (D-068).</summary>
    ClassCancelled,
}

/// <summary>
/// One account's spot in a class session. Booking needs an account, not the gym membership
/// (D-039), so it belongs to the account. A cancelled booking stays as history.
/// </summary>
internal sealed class ClassBooking
{
    private ClassBooking()
    {
    }

    public ClassBookingId Id { get; private set; }

    public ClassSessionId ClassSessionId { get; private set; }

    public Guid AccountId { get; private set; }

    public ClassBookingStatus Status { get; private set; }

    public Instant BookedAt { get; private set; }

    public Instant? CancelledAt { get; private set; }

    /// <summary>
    /// When the class last moved to a new time while this booking was active, else null. The
    /// member may then cancel until the class starts, even after the 12-hour deadline (D-068).
    /// </summary>
    public Instant? ClassMovedAt { get; private set; }

    public bool IsActive => Status == ClassBookingStatus.Booked;

    internal static ClassBooking Create(ClassSessionId sessionId, Guid accountId, Instant now) => new()
    {
        Id = ClassBookingId.New(),
        ClassSessionId = sessionId,
        AccountId = accountId,
        Status = ClassBookingStatus.Booked,
        BookedAt = now,
    };

    internal void Cancel(Instant now) => End(ClassBookingStatus.Cancelled, now);

    internal void CancelBecauseClassCancelled(Instant now) => End(ClassBookingStatus.ClassCancelled, now);

    internal void ClassMoved(Instant now) => ClassMovedAt = now;

    private void End(ClassBookingStatus status, Instant now)
    {
        Status = status;
        CancelledAt = now;
    }
}
