using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Identity.Endpoints;

/// <summary>The audit trail of security changes to accounts (D-072). Admins only.</summary>
internal static class AuditEndpoints
{
    /// <summary>The most events one call returns, newest first.</summary>
    public const int PageSize = 100;

    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/identity/audit-events", ListAsync).WithTags("Identity").WithName("ListAuditEvents")
            .RequireAuthorization(StaffPolicies.Admin)
            .Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status403Forbidden);
        return endpoints;
    }

    /// <param name="Id">The event's id.</param>
    /// <param name="OccurredAt">When it happened.</param>
    /// <param name="Action">What happened, e.g. <c>authenticator-reset-by-admin</c>.</param>
    /// <param name="ActorAccountId">Who did it. Null when the system did it.</param>
    /// <param name="ActorName">Their first and last name. Null when the system did it.</param>
    /// <param name="SubjectAccountId">Whose account changed.</param>
    /// <param name="SubjectName">Their first and last name.</param>
    internal sealed record AuditEntry(
        Guid Id, Instant OccurredAt, string Action, string? Detail, Guid? ActorAccountId, string? ActorName,
        Guid SubjectAccountId, string SubjectName);

    private static async Task<Ok<IReadOnlyList<AuditEntry>>> ListAsync(
        IdentityDbContext db, CancellationToken cancellationToken)
    {
        var entries = await (
                from e in db.AuditEvents
                join subject in db.Users on e.SubjectAccountId equals subject.Id
                join actor in db.Users on e.ActorAccountId equals actor.Id into actors
                from actor in actors.DefaultIfEmpty()
                orderby e.OccurredAt descending, e.Id descending
                select new AuditEntry(
                    e.Id, e.OccurredAt, e.Action, e.Detail,
                    e.ActorAccountId, actor == null ? null : actor.FirstName + " " + actor.LastName,
                    subject.Id, subject.FirstName + " " + subject.LastName))
            .Take(PageSize)
            .ToListAsync(cancellationToken);
        return TypedResults.Ok<IReadOnlyList<AuditEntry>>(entries);
    }
}
