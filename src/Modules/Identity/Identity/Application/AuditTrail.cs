using FitnessCenter.Modules.Identity.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Identity.Application;

/// <summary>
/// Records account changes in the audit trail (D-072). Callers pass ids, an
/// <see cref="AuditActions"/> name and at most a non-personal detail, never a changed value.
/// </summary>
internal sealed class AuditTrail(IdentityDbContext db, IClock clock)
{
    /// <summary>Adds the event to the context, to be saved with the caller's own changes.</summary>
    public void Add(Guid actorId, Guid subjectId, string action, string? detail = null) =>
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.CreateVersion7(),
            OccurredAt = clock.GetCurrentInstant(),
            ActorAccountId = actorId,
            SubjectAccountId = subjectId,
            Action = action,
            Detail = detail,
        });

    /// <summary>Adds the event and saves it now.</summary>
    public async Task RecordAsync(
        Guid actorId, Guid subjectId, string action, string? detail, CancellationToken cancellationToken)
    {
        Add(actorId, subjectId, action, detail);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>How long an event is kept (owner, 2026-11-26, D-072 amendment): one calendar year.</summary>
    public static readonly Period Retention = Period.FromYears(1);

    /// <summary>
    /// The oldest time still kept: the same date and time one year ago, in Copenhagen (CenterTime).
    /// Events from before it are deleted.
    /// </summary>
    public static Instant KeepFrom(Instant now) =>
        now.InZone(CenterTime.Zone).LocalDateTime.Minus(Retention)
            .InZoneLeniently(CenterTime.Zone).ToInstant();

    /// <summary>Deletes the events older than the retention period. The only deletes the trail allows.</summary>
    public Task<int> PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        var keepFrom = KeepFrom(clock.GetCurrentInstant());
        return db.AuditEvents.Where(e => e.OccurredAt < keepFrom).ExecuteDeleteAsync(cancellationToken);
    }
}
