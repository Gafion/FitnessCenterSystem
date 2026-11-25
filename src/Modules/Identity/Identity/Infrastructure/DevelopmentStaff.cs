using System.Text.RegularExpressions;
using FitnessCenter.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FitnessCenter.Modules.Identity.Infrastructure;

/// <summary>
/// One synthetic staff account per role (D-049), to click through locally. Development only,
/// never real people (CLAUDE.md). The password comes from development configuration
/// (user-secrets), so none is committed. Real staff accounts come from an admin's invite (D-058).
/// An optional authenticator key turns the authenticator on for them, so the full-stack E2E
/// tests can compute the login code (FIT-109).
/// </summary>
internal static partial class DevelopmentStaff
{
    public const string PasswordKey = "Identity:DevelopmentStaffPassword";

    /// <summary>
    /// A Base32 TOTP key (D-059). When set, every seeded account gets it with the authenticator
    /// on, so a test can compute the code. Without it, staff set up the app themselves.
    /// </summary>
    public const string AuthenticatorKeyKey = "Identity:DevelopmentStaffAuthenticatorKey";

    // Synthetic names on the reserved example.com domain.
    public static IReadOnlyList<(string Role, string Email, string FirstName)> Accounts { get; } =
    [
        (StaffRoles.Admin, "dev-admin@example.com", "Admin"),
        (StaffRoles.Manager, "dev-manager@example.com", "Manager"),
        (StaffRoles.FrontDesk, "dev-frontdesk@example.com", "Frontdesk"),
        (StaffRoles.Instructor, "dev-instructor@example.com", "Instructor"),
    ];

    /// <summary>Creates the accounts that are missing and gives each its role. Safe to run on every start.</summary>
    public static async Task EnsureAsync(
        UserManager<Account> users, IUserStore<Account> store, string? password, string? authenticatorKey, ILogger logger)
    {
        if (string.IsNullOrEmpty(password))
        {
            logger.LogInformation(
                "No development staff seeded: set {Key} with dotnet user-secrets to get one account per staff role.",
                PasswordKey);
            return;
        }

        var key = NormalizeKey(authenticatorKey);
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

            if (key is not null)
            {
                await EnsureAuthenticatorAsync(users, store, account, key, email);
            }
        }
    }

    /// <summary>Gives the account the configured key with the authenticator on, unless it already has both.</summary>
    private static async Task EnsureAuthenticatorAsync(
        UserManager<Account> users, IUserStore<Account> store, Account account, string key, string email)
    {
        if (account.TwoFactorEnabled && await users.GetAuthenticatorKeyAsync(account) == key)
        {
            return;
        }

        // UserManager can only reset the key to a random one; the store sets a chosen one.
        var keys = (IUserAuthenticatorKeyStore<Account>)store;
        await keys.SetAuthenticatorKeyAsync(account, key, CancellationToken.None);
        Check(await users.SetTwoFactorEnabledAsync(account, true), email); // also saves the key
    }

    /// <summary>
    /// Upper case without spaces, as Identity stores keys. At least 160 bits (32 Base32 characters),
    /// the size Identity generates, so a typo fails at startup rather than at login.
    /// </summary>
    private static string? NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var normalized = key.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
        if (!Base32Key().IsMatch(normalized))
        {
            throw new InvalidOperationException(
                $"{AuthenticatorKeyKey} must be at least 32 Base32 characters (A-Z, 2-7).");
        }

        return normalized;
    }

    [GeneratedRegex("^[A-Z2-7]{32,}$")]
    private static partial Regex Base32Key();

    private static void Check(IdentityResult result, string email)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seeding development staff {email} failed: {string.Join(", ", result.Errors.Select(e => e.Code))}");
        }
    }
}
