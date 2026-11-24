using FitnessCenter.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FitnessCenter.Modules.Identity.Infrastructure;

/// <summary>
/// One synthetic staff account per role (D-049), to click through locally. Development only,
/// never real people (CLAUDE.md). The password comes from development configuration
/// (user-secrets), so none is committed. How real staff accounts are created is open (FIT-75).
/// </summary>
internal static class DevelopmentStaff
{
    public const string PasswordKey = "Identity:DevelopmentStaffPassword";

    // Synthetic names on the reserved example.com domain.
    public static IReadOnlyList<(string Role, string Email, string FirstName)> Accounts { get; } =
    [
        (StaffRoles.Admin, "dev-admin@example.com", "Admin"),
        (StaffRoles.Manager, "dev-manager@example.com", "Manager"),
        (StaffRoles.FrontDesk, "dev-frontdesk@example.com", "Frontdesk"),
        (StaffRoles.Instructor, "dev-instructor@example.com", "Instructor"),
    ];

    /// <summary>Creates the accounts that are missing and gives each its role. Safe to run on every start.</summary>
    public static async Task EnsureAsync(UserManager<Account> users, string? password, ILogger logger)
    {
        if (string.IsNullOrEmpty(password))
        {
            logger.LogInformation(
                "No development staff seeded: set {Key} with dotnet user-secrets to get one account per staff role.",
                PasswordKey);
            return;
        }

        foreach (var (role, email, firstName) in Accounts)
        {
            var account = await users.FindByEmailAsync(email);
            if (account is null)
            {
                account = new Account
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true, // no mailbox round trip for seed data (D-038)
                    FirstName = firstName,
                    LastName = "Dev",
                };
                Check(await users.CreateAsync(account, password), email);
            }

            if (!await users.IsInRoleAsync(account, role))
            {
                Check(await users.AddToRoleAsync(account, role), email);
            }
        }
    }

    private static void Check(IdentityResult result, string email)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seeding development staff {email} failed: {string.Join(", ", result.Errors.Select(e => e.Code))}");
        }
    }
}
