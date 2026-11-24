using NodaTime;

namespace FitnessCenter.Modules.Members.Domain;

/// <summary>
/// One cancellation in a member's history (D-048, D-055). Undoing it sets
/// <see cref="UndoneAt"/>; the record is never deleted (FIT-95).
/// </summary>
internal sealed class CancellationRecord
{
    public CancellationRecord(CancellationRecordId id, MemberId memberId, Instant cancelledAt, LocalDate effectiveOn)
    {
        Id = id;
        MemberId = memberId;
        CancelledAt = cancelledAt;
        EffectiveOn = effectiveOn;
    }

    public CancellationRecordId Id { get; private set; }

    public MemberId MemberId { get; private set; }

    /// <summary>When the member cancelled (UTC).</summary>
    public Instant CancelledAt { get; private set; }

    /// <summary>The last day of access the cancellation set (Copenhagen).</summary>
    public LocalDate EffectiveOn { get; private set; }

    /// <summary>When the member undid the cancellation (UTC); null while it stands.</summary>
    public Instant? UndoneAt { get; private set; }

    public void Undo(Instant now) => UndoneAt = now;
}
