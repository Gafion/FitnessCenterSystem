using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace FitnessCenter.Modules.Identity.Endpoints;

/// <summary>
/// Lists staff by role, e.g. the instructors for the instructor picker on the class form (FIT-105).
/// Returns the minimum data: account id and display name, sorted by name (ordinal, ignoring case).
/// </summary>
internal static class StaffEndpoints
{
    public static IEndpointRouteBuilder MapStaffEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // The Manager policy admits admins too (D-062).
        endpoints.MapGet("/api/identity/staff", ListByRoleAsync).WithTags("Identity").WithName("ListStaffByRole")
            .RequireAuthorization(StaffPolicies.Manager)
            .Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    /// <param name="Id">The staff member's account id.</param>
    /// <param name="DisplayName">First and last name.</param>
    internal sealed record StaffMember(Guid Id, string DisplayName);

    /// <param name="role">One of the staff roles: Admin, Manager, FrontDesk or Instructor. Required; missing or
    /// unknown gives 400.</param>
    /// <param name="users">The account store.</param>
    private static async Task<Results<Ok<IReadOnlyList<StaffMember>>, ValidationProblem>> ListByRoleAsync(
        string? role, UserManager<Account> users)
    {
        var knownRole = StaffRoles.All.FirstOrDefault(known => string.Equals(known, role, StringComparison.Ordinal));
        if (knownRole is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["role"] = [$"Role must be one of: {string.Join(", ", StaffRoles.All)}."],
            });
        }

        var accounts = await users.GetUsersInRoleAsync(knownRole);
        IReadOnlyList<StaffMember> staff = accounts
            .Select(account => new StaffMember(account.Id, $"{account.FirstName} {account.LastName}"))
            .OrderBy(member => member.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(member => member.Id)
            .ToList();
        return TypedResults.Ok(staff);
    }
}
