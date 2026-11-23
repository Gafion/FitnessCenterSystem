using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace FitnessCenter.Modules.Identity.Application;

/// <summary>Names and checks shared by the Google login code (D-011, D-037).</summary>
internal static class ExternalLogins
{
    public const string Google = "Google";

    /// <summary>Google's OIDC <c>email_verified</c> claim, mapped from its userinfo response.</summary>
    public const string EmailVerifiedClaim = "email_verified";

    public static string? Email(ExternalLoginInfo login) => login.Principal.FindFirstValue(ClaimTypes.Email);

    /// <summary>
    /// A new account takes the email from Google only when Google says the person controls it;
    /// otherwise someone could claim an address they don't own.
    /// </summary>
    public static bool EmailIsVerified(ExternalLoginInfo login) =>
        string.Equals(login.Principal.FindFirstValue(EmailVerifiedClaim), "true", StringComparison.OrdinalIgnoreCase);
}
