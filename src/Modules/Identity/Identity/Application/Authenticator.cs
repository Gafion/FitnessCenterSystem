using System.Text;
using System.Text.Encodings.Web;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FitnessCenter.Modules.Identity.Application;

/// <summary>
/// Authenticator-app MFA with time-based one-time codes (TOTP, RFC 6238), through ASP.NET Core
/// Identity's built-in authenticator support (D-059). Only standard pieces: the
/// <c>otpauth://totp/</c> URI for the QR code and the Base32 setup key, so any TOTP app works
/// (Google Authenticator, Microsoft Authenticator, 1Password).
/// </summary>
internal static class Authenticator
{
    /// <summary>Shown as the account's label in the authenticator app. Placeholder name (D-044).</summary>
    public const string Issuer = "FitnessCenter";

    public const int RecoveryCodeCount = 10;

    /// <summary>The setup key in groups of four, easier to type by hand.</summary>
    public static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        for (var i = 0; i < unformattedKey.Length; i += 4)
        {
            if (i > 0)
            {
                result.Append(' ');
            }

            result.Append(unformattedKey.AsSpan(i, Math.Min(4, unformattedKey.Length - i)));
        }

        return result.ToString().ToLowerInvariant();
    }

    /// <summary>The key-URI format TOTP apps read from a QR code: otpauth://totp/Issuer:label?secret=…&amp;issuer=…</summary>
    public static string Uri(string email, string unformattedKey)
    {
        var issuer = UrlEncoder.Default.Encode(Issuer);
        return $"otpauth://totp/{issuer}:{UrlEncoder.Default.Encode(email)}?secret={unformattedKey}&issuer={issuer}&digits=6";
    }

    /// <summary>App codes are typed by hand or pasted, so spaces and hyphens are ignored.</summary>
    public static string NormalizeAppCode(string code) =>
        NormalizeRecoveryCode(code).Replace("-", "", StringComparison.Ordinal);

    /// <summary>Recovery codes keep their hyphen (Identity stores them as XXXXX-XXXXX); only spaces are ignored.</summary>
    public static string NormalizeRecoveryCode(string code) => code.Trim().Replace(" ", "", StringComparison.Ordinal);

    public static async Task<bool> IsStaffAsync(UserManager<Account> users, Account account)
    {
        var roles = await users.GetRolesAsync(account);
        return roles.Any(StaffRoles.All.Contains);
    }
}

/// <summary>Staff endpoints need the authenticator set up first (D-013, D-059). Members never match a staff policy anyway.</summary>
internal sealed class AuthenticatorSetUpRequirement : IAuthorizationRequirement;

internal sealed class AuthenticatorSetUpHandler(UserManager<Account> users) : AuthorizationHandler<AuthenticatorSetUpRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AuthenticatorSetUpRequirement requirement)
    {
        // Read from the database on each request, not from the cookie, so a session from before
        // the setup counts only once the authenticator is really on.
        if (await users.GetUserAsync(context.User) is { TwoFactorEnabled: true })
        {
            context.Succeed(requirement);
        }
    }
}
