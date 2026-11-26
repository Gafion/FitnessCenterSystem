namespace FitnessCenter.Modules.Identity.Contracts;

/// <summary>
/// The shared audit trail (D-072): every module records the actions it takes on a person here.
/// Sanitized: pass account ids, an action name and at most a non-personal detail (an id, a role
/// or a method), never a changed value, name, email, amount or code.
/// </summary>
public interface IAuditLog
{
    /// <param name="actorAccountId">Who did it. Null when the system did it on its own (a scheduled job).</param>
    /// <param name="subjectAccountId">Whose account, membership, booking or check-in it was.</param>
    /// <param name="action">A kebab-case name such as <c>class-booked</c>.</param>
    /// <param name="detail">A non-personal detail, such as a class id. Null when there is none.</param>
    Task RecordAsync(
        Guid? actorAccountId, Guid subjectAccountId, string action, string? detail, CancellationToken cancellationToken);
}
