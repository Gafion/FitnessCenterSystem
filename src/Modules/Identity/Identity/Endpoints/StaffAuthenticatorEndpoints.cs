using System.Security.Claims;
using FitnessCenter.Modules.Identity.Application;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace FitnessCenter.Modules.Identity.Endpoints;

/// <summary>
/// Admins reset a staff member's authenticator app when it is lost (D-071). Admins only, not
/// managers, and never their own. The staff member sets it up again at the next login (D-059).
/// </summary>
internal static class StaffAuthenticatorEndpoints
{
    public static IEndpointRouteBuilder MapStaffAuthenticatorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/identity/staff-accounts").WithTags("Identity")
            .RequireAuthorization(StaffPolicies.Admin);

        group.MapGet("/", ListAsync).WithName("ListStaffAccounts")
            .Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status403Forbidden);
        group.MapDelete("/{accountId:guid}/authenticator", ResetAsync).WithName("ResetStaffAuthenticator")
            .Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    /// <param name="Id">The staff member's account id.</param>
    /// <param name="DisplayName">First and last name.</param>
    /// <param name="Email">The login email, to tell two people with the same name apart.</param>
    /// <param name="StaffRoles">The account's staff roles (D-049).</param>
    /// <param name="AuthenticatorEnabled">The authenticator app is set up (D-059). Only then is there anything to reset.</param>
    internal sealed record StaffAccount(
        Guid Id, string DisplayName, string Email, IReadOnlyList<string> StaffRoles, bool AuthenticatorEnabled);

    /// <summary>Every account with a staff role, sorted by name (ordinal, ignoring case), for the admin to pick from.</summary>
    private static async Task<Ok<IReadOnlyList<StaffAccount>>> ListAsync(UserManager<Account> users)
    {
        var byId = new Dictionary<Guid, (Account Account, List<string> Roles)>();
        foreach (var role in StaffRoles.All)
        {
            foreach (var account in await users.GetUsersInRoleAsync(role))
            {
                if (!byId.TryGetValue(account.Id, out var entry))
                {
                    entry = (account, []);
                    byId[account.Id] = entry;
                }

                entry.Roles.Add(role);
            }
        }

        IReadOnlyList<StaffAccount> staff = byId.Values
            .Select(entry => new StaffAccount(
                entry.Account.Id, $"{entry.Account.FirstName} {entry.Account.LastName}", entry.Account.Email!,
                entry.Roles, entry.Account.TwoFactorEnabled))
            .OrderBy(account => account.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(account => account.Id)
            .ToList();
        return TypedResults.Ok(staff);
    }

    /// <summary>
    /// Turns the staff member's authenticator off and removes its key and recovery codes, then emails
    /// them. Recorded in the audit trail with both account ids (D-072).
    /// </summary>
    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> ResetAsync(
        Guid accountId, ClaimsPrincipal user, UserManager<Account> users, AuthenticatorRemoval removal,
        ILogger<AuthenticatorRemoval> logger, CancellationToken cancellationToken)
    {
        var adminId = users.GetUserId(user);
        if (string.Equals(adminId, accountId.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.Problem(
                title: "You can't reset your own authenticator app.",
                detail: "Ask another administrator to do it.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "cannot-reset-own-authenticator");
        }

        // Members aren't listed or reset here: a member turns their own off (D-071).
        if (await users.FindByIdAsync(accountId.ToString()) is not { } account
            || !await Authenticator.IsStaffAsync(users, account))
        {
            return TypedResults.NotFound();
        }

        if (!account.TwoFactorEnabled)
        {
            return TypedResults.Problem(
                title: "This staff member hasn't set up the authenticator app yet.",
                detail: "They'll be asked to set it up at their next login.",
                statusCode: StatusCodes.Status409Conflict,
                type: "authenticator-not-enabled");
        }

        await removal.RemoveAsync(
            account, AuthenticatorRemoval.Reason.ResetByAdmin, Guid.Parse(adminId!), cancellationToken);
        logger.LogInformation(
            "Admin {AdminId} reset the authenticator app of staff account {AccountId}", adminId, account.Id);
        return TypedResults.NoContent();
    }
}
