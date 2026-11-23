using NodaTime;

namespace FitnessCenter.Modules.Access.Domain;

/// <summary>
/// One accepted swipe at the entrance (D-003, data-model.md ATTENDANCE). How a swipe is
/// linked to a class or PT session is still open (D-003), so every attendance is a gym visit.
/// </summary>
internal sealed class Attendance(AttendanceId id, AccessCardId accessCardId, Guid memberId, Instant swipedAt)
{
    public AttendanceId Id { get; private set; } = id;

    public AccessCardId AccessCardId { get; private set; } = accessCardId;

    /// <summary>The card's member at the time, kept so the history survives a card changing hands later.</summary>
    public Guid MemberId { get; private set; } = memberId;

    public Instant SwipedAt { get; private set; } = swipedAt;
}
