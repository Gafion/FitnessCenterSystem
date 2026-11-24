using System.Security.Claims;
using FitnessCenter.Modules.Identity.Application;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace FitnessCenter.Modules.Identity.Endpoints;

/// <summary>Staff invites (D-058): only an admin invites; the invited person accepts by link.</summary>
internal static class StaffInviteEndpoints
{
    public static IEndpointRouteBuilder MapStaffInviteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/identity/staff-invites").WithTags("Identity");

        // Only admins invite, for now (D-058). Whether managers may invite too is open.
        group.MapPost("/", InviteAsync).WithName("InviteStaff").RequireAuthorization(StaffPolicies.Admin)
            .Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status403Forbidden);
        group.MapPost("/accept", AcceptAsync).WithName("AcceptStaffInvite");

        return endpoints;
    }

    internal sealed record InviteStaffRequest(string Email, string Role);

    internal sealed record StaffInviteResponse(Guid InviteId, string Email, string Role, DateTimeOffset ExpiresAt);

    internal sealed record AcceptStaffInviteRequest(
        Guid InviteId, string Code, string FirstName, string LastName, string Password);

    private static async Task<Results<Created<StaffInviteResponse>, ValidationProblem, ProblemHttpResult>> InviteAsync(
        InviteStaffRequest request, ClaimsPrincipal user, UserManager<Account> users, StaffInvites invites,
        HttpContext context, CancellationToken cancellationToken)
    {
        var adminId = Guid.Parse(users.GetUserId(user)
            ?? throw new InvalidOperationException("An authorized admin has no account id."));
        var outcome = await invites.InviteAsync(adminId, request.Email, request.Role, context.Request, cancellationToken);

        if (outcome.Errors is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (outcome.Invite is not { } invite)
        {
            return TypedResults.Problem(
                title: "An account with this email already exists.",
                detail: "Staff accounts are separate from member accounts; use another email for now.",
                statusCode: StatusCodes.Status409Conflict,
                type: "account-exists");
        }

        return TypedResults.Created(
            (string?)null,
            new StaffInviteResponse(invite.Id, invite.Email, invite.Role, invite.ExpiresAt.ToDateTimeOffset()));
    }

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> AcceptAsync(
        AcceptStaffInviteRequest request, StaffInvites invites, CancellationToken cancellationToken)
    {
        var outcome = await invites.AcceptAsync(
            request.InviteId, request.Code, request.FirstName, request.LastName, request.Password, cancellationToken);

        return outcome.Status switch
        {
            AcceptStatus.Accepted => TypedResults.NoContent(),
            AcceptStatus.Invalid => TypedResults.ValidationProblem(outcome.Errors!),
            AcceptStatus.Used => Problem(
                "This invite has already been used.", "Log in with the email and password you chose.",
                StatusCodes.Status409Conflict, "invite-used"),
            AcceptStatus.Expired => Problem(
                "This invite has expired.", "Ask an admin to send a new invite.",
                StatusCodes.Status410Gone, "invite-expired"),
            AcceptStatus.AccountExists => Problem(
                "An account with this email already exists.", "Ask an admin to invite another email.",
                StatusCodes.Status409Conflict, "account-exists"),
            _ => Problem(
                "This invite link doesn't work.", "Check that you used the whole link from the email.",
                StatusCodes.Status400BadRequest, "invalid-invite"),
        };
    }

    private static ProblemHttpResult Problem(string title, string detail, int status, string type) =>
        TypedResults.Problem(title: title, detail: detail, statusCode: status, type: type);
}
