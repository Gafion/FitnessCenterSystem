using FitnessCenter.Modules.Identity.Infrastructure;
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
}
