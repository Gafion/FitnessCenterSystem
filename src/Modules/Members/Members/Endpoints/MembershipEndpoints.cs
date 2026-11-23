using System.Security.Claims;
using FitnessCenter.Modules.Members.Application;
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

        return endpoints;
    }

    /// <param name="Status">"AwaitingFirstPayment" until the first payment, then "Active".</param>
    /// <param name="MemberSince">The signup date (Copenhagen).</param>
    /// <param name="PaidUntil">Last day covered by a payment; null before the first one (D-020).</param>
    internal sealed record MyMembership(
        string MembershipType, long MonthlyPriceOre, string Status, DateOnly MemberSince, DateOnly? PaidUntil);

    private static async Task<Results<Ok<MyMembership>, NotFound>> GetMyMembershipAsync(
        ClaimsPrincipal user, MembershipHandler handler, CancellationToken cancellationToken)
    {
        var accountId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var membership = await handler.GetAsync(accountId, cancellationToken);
        return membership is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new MyMembership(
                membership.MembershipType.Name,
                membership.MembershipType.MonthlyPrice.Ore,
                membership.Member.Status.ToString(),
                membership.Member.MemberSince.ToDateOnly(),
                membership.Member.PaidUntil?.ToDateOnly()));
    }
}
