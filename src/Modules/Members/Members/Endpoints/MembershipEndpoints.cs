using System.Security.Claims;
using FitnessCenter.Modules.Members.Application;
using FitnessCenter.Modules.Members.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FitnessCenter.Modules.Members.Endpoints;

internal static class MembershipEndpoints
{
    public static IEndpointRouteBuilder MapMembershipEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/members").WithTags("Members")
            .MapGet("/me", GetMyMembershipAsync).WithName("GetMyMembership").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);

        // No retention step before the cancellation (D-048): one request cancels.
        endpoints.MapGroup("/api/members").WithTags("Members")
            .MapPost("/me/cancellation", CancelMyMembershipAsync).WithName("CancelMyMembership").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);

        // Undo is allowed up to and including the effective date (D-055).
        endpoints.MapGroup("/api/members").WithTags("Members")
            .MapDelete("/me/cancellation", UndoMyCancellationAsync).WithName("UndoMyCancellation").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    /// <param name="Status">"AwaitingFirstPayment" until the first payment, then "Active".</param>
    /// <param name="MemberSince">The signup date (Copenhagen).</param>
    /// <param name="PaidUntil">Last day covered by a payment; null before the first one (D-020).</param>
    /// <param name="CancelledAt">When the member cancelled; null while the membership is not cancelled.</param>
    /// <param name="CancellationEffectiveOn">
    /// Last day of access (Copenhagen); the cancellation takes effect at its end (D-048). Null while not cancelled.
    /// </param>
    /// <param name="CanUndoCancellation">
    /// Whether the cancellation can still be undone today: up to and including the effective date (D-055).
    /// </param>
    internal sealed record MyMembership(
        string MembershipType,
        long MonthlyPriceOre,
        string Status,
        DateOnly MemberSince,
        DateOnly? PaidUntil,
        DateTimeOffset? CancelledAt,
        DateOnly? CancellationEffectiveOn,
        bool CanUndoCancellation);

    private static async Task<Results<Ok<MyMembership>, NotFound>> GetMyMembershipAsync(
        ClaimsPrincipal user, MembershipHandler handler, CancellationToken cancellationToken)
    {
        var accountId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var membership = await handler.GetAsync(accountId, cancellationToken);
        return membership is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(membership, handler.Today));
    }

    /// <summary>Cancels the membership; 409 when it is already cancelled, which changes nothing (FIT-70).</summary>
    private static async Task<Results<Ok<MyMembership>, NotFound, ProblemHttpResult>> CancelMyMembershipAsync(
        ClaimsPrincipal user, MembershipHandler handler, CancellationToken cancellationToken)
    {
        var accountId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await handler.CancelAsync(accountId, cancellationToken);
        return result switch
        {
            null => TypedResults.NotFound(),
            { Outcome: CancellationOutcome.AlreadyCancelled } => TypedResults.Problem(
                "The membership is already cancelled.", statusCode: StatusCodes.Status409Conflict),
            _ => TypedResults.Ok(ToResponse(result.Membership, handler.Today)),
        };
    }

    /// <summary>
    /// Undoes a pending cancellation. With nothing pending it changes nothing and returns the
    /// membership; 409 after the effective date, when the membership has ended (D-055).
    /// </summary>
    private static async Task<Results<Ok<MyMembership>, NotFound, ProblemHttpResult>> UndoMyCancellationAsync(
        ClaimsPrincipal user, MembershipHandler handler, CancellationToken cancellationToken)
    {
        var accountId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await handler.UndoCancellationAsync(accountId, cancellationToken);
        return result switch
        {
            null => TypedResults.NotFound(),
            { Outcome: UndoCancellationOutcome.AlreadyEffective } => TypedResults.Problem(
                "The cancellation has taken effect; the membership has ended.", statusCode: StatusCodes.Status409Conflict),
            _ => TypedResults.Ok(ToResponse(result.Membership, handler.Today)),
        };
    }

    private static MyMembership ToResponse(MembershipSummary membership, NodaTime.LocalDate today) => new(
        membership.MembershipType.Name,
        membership.MembershipType.MonthlyPrice.Ore,
        membership.Member.Status.ToString(),
        membership.Member.MemberSince.ToDateOnly(),
        membership.Member.PaidUntil?.ToDateOnly(),
        membership.Member.CancelledAt?.ToDateTimeOffset(),
        membership.Member.CancellationEffectiveOn?.ToDateOnly(),
        membership.Member.CanUndoCancellationOn(today));
}
