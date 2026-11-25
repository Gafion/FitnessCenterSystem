using System.Security.Claims;
using FitnessCenter.Modules.Identity.Application;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace FitnessCenter.Modules.Identity.Endpoints;

/// <summary>
/// Staff invites (D-058): admins and managers invite (D-063); the invited person accepts by link,
/// either as a new account or, if the email has an account already, by logging in (D-064).
/// </summary>
internal static class StaffInviteEndpoints
{
    public static IEndpointRouteBuilder MapStaffInviteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/identity/staff-invites").WithTags("Identity");

        // The Manager policy admits admins too (D-062); which roles each may give is D-063.
        group.MapPost("/", InviteAsync).WithName("InviteStaff").RequireAuthorization(StaffPolicies.Manager)
            .Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status403Forbidden);

        // The roles the logged-in inviter may give (D-063), so the invite screen offers only those.
        group.MapGet("/roles", GetInvitableRoles).WithName("GetInvitableRoles").RequireAuthorization(StaffPolicies.Manager)
            .Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status403Forbidden);
        group.MapPost("/accept", AcceptAsync).WithName("AcceptStaffInvite");

        // Any logged-in account: the invite says which email it is for (D-064).
        group.MapPost("/accept-existing", AcceptExistingAsync).WithName("AcceptStaffInviteForAccount")
            .RequireAuthorization().Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    /// <param name="Language">
    /// The language of the invite email to an email without an account: "da" or "en" (D-065).
    /// Left out, it is the language the inviter uses the app in. An email that has an account
    /// always gets that account's language.
    /// </param>
    internal sealed record InviteStaffRequest(string Email, string Role, string? Language = null);

    /// <param name="Roles">The staff roles the logged-in inviter may give (D-063).</param>
    internal sealed record InvitableRoles(IReadOnlyList<string> Roles);

    internal sealed record StaffInviteResponse(Guid InviteId, string Email, string Role, DateTimeOffset ExpiresAt);

    internal sealed record AcceptStaffInviteForAccountRequest(Guid InviteId, string Code);

    internal sealed record AcceptStaffInviteRequest(
        Guid InviteId, string Code, string FirstName, string LastName, string Password);

    private static async Task<Results<Created<StaffInviteResponse>, ValidationProblem, ProblemHttpResult>> InviteAsync(
        InviteStaffRequest request, ClaimsPrincipal user, UserManager<Account> users, StaffInvites invites,
        HttpContext context, CancellationToken cancellationToken)
    {
        var inviterId = Guid.Parse(users.GetUserId(user)
            ?? throw new InvalidOperationException("An authorized inviter has no account id."));
        var inviterRoles = StaffRoles.All.Where(user.IsInRole).ToList();
        var outcome = await invites.InviteAsync(
            inviterId, inviterRoles, request.Email, request.Role, request.Language,
            Languages.FromAcceptLanguage(context.Request.Headers.AcceptLanguage), context.Request, cancellationToken);

        switch (outcome.Status)
        {
            case InviteStatus.Invalid:
                return TypedResults.ValidationProblem(outcome.Errors!);
            case InviteStatus.RoleNotAllowed:
                return Problem(
                    "You may not give this role.", "A manager may invite to Front desk or Instructor only.",
                    StatusCodes.Status403Forbidden, "role-not-allowed");
            case InviteStatus.AlreadyHasRole:
                return Problem(
                    "This account has the role already.", "There is nothing to invite to.",
                    StatusCodes.Status409Conflict, "already-has-role");
        }

        var invite = outcome.Invite!;
        return TypedResults.Created(
            (string?)null,
            new StaffInviteResponse(invite.Id, invite.Email, invite.Role, invite.ExpiresAt.ToDateTimeOffset()));
    }

    private static Ok<InvitableRoles> GetInvitableRoles(ClaimsPrincipal user)
    {
        var inviterRoles = StaffRoles.All.Where(user.IsInRole).ToList();
        return TypedResults.Ok(new InvitableRoles(
            StaffRoles.All.Where(role => StaffRoleRules.MayGive(inviterRoles, role)).ToList()));
    }

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> AcceptAsync(
        AcceptStaffInviteRequest request, StaffInvites invites, HttpContext context, CancellationToken cancellationToken)
    {
        var outcome = await invites.AcceptAsync(
            request.InviteId, request.Code, request.FirstName, request.LastName, request.Password,
            Languages.FromAcceptLanguage(context.Request.Headers.AcceptLanguage), cancellationToken);

        return outcome.Status switch
        {
            AcceptStatus.Accepted => TypedResults.NoContent(),
            AcceptStatus.Invalid => TypedResults.ValidationProblem(outcome.Errors!),
            AcceptStatus.Used => Problem(
                "This invite has already been used.", "Log in with the email and password you chose.",
                StatusCodes.Status409Conflict, "invite-used"),
            AcceptStatus.Expired => Problem(
                "This invite has expired.", "Ask an admin or a manager to send a new invite.",
                StatusCodes.Status410Gone, "invite-expired"),
            AcceptStatus.AccountExists => Problem(
                "An account with this email already exists.", "Log in to that account, then follow the link again to accept.",
                StatusCodes.Status409Conflict, "account-exists"),
            _ => Problem(
                "This invite link doesn't work.", "Check that you used the whole link from the email.",
                StatusCodes.Status400BadRequest, "invalid-invite"),
        };
    }

    /// <summary>Adds the invite's role to the logged-in account (D-064); no new password.</summary>
    private static async Task<Results<NoContent, UnauthorizedHttpResult, ProblemHttpResult>> AcceptExistingAsync(
        AcceptStaffInviteForAccountRequest request, ClaimsPrincipal user, UserManager<Account> users,
        SignInManager<Account> signIn, StaffInvites invites, CancellationToken cancellationToken)
    {
        if (await users.GetUserAsync(user) is not { } account)
        {
            return TypedResults.Unauthorized();
        }

        var outcome = await invites.AcceptForAccountAsync(request.InviteId, request.Code, account, cancellationToken);
        if (outcome.Status == AcceptStatus.Accepted)
        {
            // The roles are in the login cookie; renew it so the new role counts at once.
            await signIn.RefreshSignInAsync(account);
            return TypedResults.NoContent();
        }

        return outcome.Status switch
        {
            AcceptStatus.Used => Problem(
                "This invite has already been used.", "Your account already has the role from it.",
                StatusCodes.Status409Conflict, "invite-used"),
            AcceptStatus.Expired => Problem(
                "This invite has expired.", "Ask an admin or a manager to send a new invite.",
                StatusCodes.Status410Gone, "invite-expired"),
            AcceptStatus.OtherAccount => Problem(
                "This invite is for another email.", "Log in with the account the invite was sent to.",
                StatusCodes.Status403Forbidden, "invite-for-another-account"),
            _ => Problem(
                "This invite link doesn't work.", "Check that you used the whole link from the email.",
                StatusCodes.Status400BadRequest, "invalid-invite"),
        };
    }

    private static ProblemHttpResult Problem(string title, string detail, int status, string type) =>
        TypedResults.Problem(title: title, detail: detail, statusCode: status, type: type);
}
