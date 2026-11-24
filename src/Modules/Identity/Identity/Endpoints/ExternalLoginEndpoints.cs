using System.Security.Claims;
using FitnessCenter.Modules.Identity.Application;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace FitnessCenter.Modules.Identity.Endpoints;

/// <summary>
/// Google login (D-011). The browser navigates to these endpoints, so they answer with
/// redirects to frontend pages, with an <c>error</c> code in the query when something failed.
/// A Google login matches an account only through a link made while logged in (D-037).
/// </summary>
internal static class ExternalLoginEndpoints
{
    private const string CallbackPath = "/api/identity/external/google/callback";
    private const string LinkCallbackPath = "/api/identity/external/google/link/callback";

    public static IEndpointRouteBuilder MapExternalLoginEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/identity").WithTags("Identity");

        group.MapGet("/login-options", GetLoginOptionsAsync).WithName("GetLoginOptions");
        group.MapGet("/external/google", StartGoogleLoginAsync).WithName("StartGoogleLogin");
        group.MapGet("/external/google/callback", FinishGoogleLoginAsync).WithName("FinishGoogleLogin");
        group.MapGet("/external/pending", GetPendingLoginAsync).WithName("GetPendingExternalLogin")
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapGet("/external/google/link", StartGoogleLinkAsync).WithName("StartGoogleLink").RequireAuthorization();
        group.MapGet("/external/google/link/callback", FinishGoogleLinkAsync).WithName("FinishGoogleLink")
            .RequireAuthorization();

        return endpoints;
    }

    internal sealed record LoginOptions(bool Google);

    /// <summary>What Google told us about the person, to prefill the rest of the signup.</summary>
    internal sealed record PendingExternalLogin(string Email, string? FirstName, string? LastName);

    private static async Task<Ok<LoginOptions>> GetLoginOptionsAsync(IAuthenticationSchemeProvider schemes) =>
        TypedResults.Ok(new LoginOptions(await GoogleIsConfiguredAsync(schemes)));

    private static async Task<Results<ChallengeHttpResult, NotFound>> StartGoogleLoginAsync(
        SignInManager<Account> signIn, IAuthenticationSchemeProvider schemes)
    {
        if (!await GoogleIsConfiguredAsync(schemes))
        {
            return TypedResults.NotFound();
        }

        var properties = signIn.ConfigureExternalAuthenticationProperties(ExternalLogins.Google, CallbackPath);
        return TypedResults.Challenge(properties, [ExternalLogins.Google]);
    }

    private static async Task<RedirectHttpResult> FinishGoogleLoginAsync(
        SignInManager<Account> signIn, UserManager<Account> users)
    {
        var login = await signIn.GetExternalLoginInfoAsync();
        if (login is null)
        {
            return Redirect("/login?error=google-failed");
        }

        var result = await signIn.ExternalLoginSignInAsync(
            login.LoginProvider, login.ProviderKey, isPersistent: true, bypassTwoFactor: false);
        if (result.Succeeded)
        {
            await signIn.Context.SignOutAsync(IdentityConstants.ExternalScheme);
            return Redirect("/account");
        }

        // The account has the authenticator on (D-059): the login page asks for the code.
        if (result.RequiresTwoFactor)
        {
            await signIn.Context.SignOutAsync(IdentityConstants.ExternalScheme);
            return Redirect("/login?step=code");
        }

        var email = ExternalLogins.Email(login);
        if (result.IsLockedOut || result.IsNotAllowed || email is null)
        {
            await signIn.Context.SignOutAsync(IdentityConstants.ExternalScheme);
            return Redirect("/login?error=google-failed");
        }

        if (!ExternalLogins.EmailIsVerified(login))
        {
            await signIn.Context.SignOutAsync(IdentityConstants.ExternalScheme);
            return Redirect("/login?error=google-unverified");
        }

        // D-037: never link by matching emails. The person logs in with their password
        // and connects Google from the account page.
        if (await users.FindByEmailAsync(email) is not null)
        {
            await signIn.Context.SignOutAsync(IdentityConstants.ExternalScheme);
            return Redirect("/login?error=google-email-taken");
        }

        // New person: the external cookie keeps the Google login while they accept the terms.
        return Redirect("/signup/google");
    }

    private static async Task<Results<Ok<PendingExternalLogin>, UnauthorizedHttpResult>> GetPendingLoginAsync(
        SignInManager<Account> signIn)
    {
        var login = await signIn.GetExternalLoginInfoAsync();
        var email = login is null ? null : ExternalLogins.Email(login);
        if (login is null || email is null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new PendingExternalLogin(
            email,
            login.Principal.FindFirstValue(ClaimTypes.GivenName),
            login.Principal.FindFirstValue(ClaimTypes.Surname)));
    }

    private static async Task<Results<ChallengeHttpResult, NotFound>> StartGoogleLinkAsync(
        ClaimsPrincipal user, SignInManager<Account> signIn, UserManager<Account> users,
        IAuthenticationSchemeProvider schemes)
    {
        if (!await GoogleIsConfiguredAsync(schemes))
        {
            return TypedResults.NotFound();
        }

        // The account id goes into the login's state, so the callback only links the
        // Google account to the person who started the link.
        var properties = signIn.ConfigureExternalAuthenticationProperties(
            ExternalLogins.Google, LinkCallbackPath, users.GetUserId(user));
        return TypedResults.Challenge(properties, [ExternalLogins.Google]);
    }

    private static async Task<RedirectHttpResult> FinishGoogleLinkAsync(
        ClaimsPrincipal user, SignInManager<Account> signIn, UserManager<Account> users)
    {
        var account = await users.GetUserAsync(user);
        var login = account is null ? null : await signIn.GetExternalLoginInfoAsync(account.Id.ToString());
        await signIn.Context.SignOutAsync(IdentityConstants.ExternalScheme);
        if (account is null || login is null)
        {
            return Redirect("/account?error=google-failed");
        }

        var result = await users.AddLoginAsync(account, login);
        return result.Succeeded
            ? Redirect("/account")
            // The only expected failure: this Google account belongs to another account.
            : Redirect("/account?error=google-in-use");
    }

    private static async Task<bool> GoogleIsConfiguredAsync(IAuthenticationSchemeProvider schemes) =>
        await schemes.GetSchemeAsync(ExternalLogins.Google) is not null;

    private static RedirectHttpResult Redirect(string path) => TypedResults.LocalRedirect(path);
}
