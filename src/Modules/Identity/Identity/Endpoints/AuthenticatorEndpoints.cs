using System.Security.Claims;
using FitnessCenter.Modules.Identity.Application;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace FitnessCenter.Modules.Identity.Endpoints;

/// <summary>
/// Setting up the authenticator app (D-059): mandatory for staff, optional for members (D-013).
/// The login code step is in <see cref="AccountEndpoints"/>.
/// </summary>
internal static class AuthenticatorEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticatorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/identity/authenticator").WithTags("Identity")
            .RequireAuthorization();

        group.MapGet("/", GetSetupAsync).WithName("GetAuthenticatorSetup")
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapPost("/", EnableAsync).WithName("EnableAuthenticator")
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    /// <summary>
    /// The QR code's URI and the setup key, while the authenticator isn't on yet. Once it is on,
    /// the key is never shown again.
    /// </summary>
    internal sealed record AuthenticatorSetup(bool Enabled, string? SharedKey, string? AuthenticatorUri);

    internal sealed record EnableAuthenticatorRequest(string Code);

    /// <summary>Shown once, right after setup. Each code logs in once instead of an app code.</summary>
    internal sealed record RecoveryCodes(IReadOnlyList<string> Codes);

    private static async Task<Results<Ok<AuthenticatorSetup>, UnauthorizedHttpResult>> GetSetupAsync(
        ClaimsPrincipal user, UserManager<Account> users)
    {
        if (await users.GetUserAsync(user) is not { } account)
        {
            return TypedResults.Unauthorized();
        }

        if (account.TwoFactorEnabled)
        {
            return TypedResults.Ok(new AuthenticatorSetup(true, null, null));
        }

        var key = await users.GetAuthenticatorKeyAsync(account);
        if (string.IsNullOrEmpty(key))
        {
            await users.ResetAuthenticatorKeyAsync(account);
            key = await users.GetAuthenticatorKeyAsync(account);
        }

        return TypedResults.Ok(new AuthenticatorSetup(
            false, Authenticator.FormatKey(key!), Authenticator.Uri(account.Email!, key!)));
    }

    /// <summary>Turns the authenticator on once a code from the app matches the key, and returns new recovery codes.</summary>
    private static async Task<Results<Ok<RecoveryCodes>, UnauthorizedHttpResult, ProblemHttpResult>> EnableAsync(
        EnableAuthenticatorRequest request, ClaimsPrincipal user, UserManager<Account> users,
        SignInManager<Account> signIn)
    {
        if (await users.GetUserAsync(user) is not { } account)
        {
            return TypedResults.Unauthorized();
        }

        if (account.TwoFactorEnabled)
        {
            return TypedResults.Problem(
                title: "The authenticator app is already set up.",
                statusCode: StatusCodes.Status409Conflict,
                type: "authenticator-already-enabled");
        }

        var valid = await users.GetAuthenticatorKeyAsync(account) is not null
            && await users.VerifyTwoFactorTokenAsync(
                account, users.Options.Tokens.AuthenticatorTokenProvider, Authenticator.NormalizeAppCode(request.Code));
        if (!valid)
        {
            return TypedResults.Problem(
                title: "That code doesn't match.",
                detail: "Check that the app's clock is right, and type the code it shows now.",
                statusCode: StatusCodes.Status400BadRequest,
                type: "invalid-code");
        }

        await users.SetTwoFactorEnabledAsync(account, true);
        var codes = await users.GenerateNewTwoFactorRecoveryCodesAsync(account, Authenticator.RecoveryCodeCount);

        // Setting up changes the security stamp; renew the cookie so this session stays valid.
        await signIn.RefreshSignInAsync(account);
        return TypedResults.Ok(new RecoveryCodes([.. codes!]));
    }
}
