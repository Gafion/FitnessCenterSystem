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
        group.MapPost("/turn-off", TurnOffAsync).WithName("TurnOffAuthenticator")
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    /// <summary>
    /// The QR code's URI and the setup key, while the authenticator isn't on yet. Once it is on,
    /// the key is never shown again.
    /// </summary>
    internal sealed record AuthenticatorSetup(bool Enabled, string? SharedKey, string? AuthenticatorUri);

    internal sealed record EnableAuthenticatorRequest(string Code);

    /// <param name="Code">A code from the authenticator app, or a recovery code.</param>
    /// <param name="IsRecoveryCode">True when <paramref name="Code"/> is a recovery code.</param>
    internal sealed record TurnOffAuthenticatorRequest(string Code, bool IsRecoveryCode);

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
        SignInManager<Account> signIn, AuditTrail audit, CancellationToken cancellationToken)
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
        await audit.RecordAsync(account.Id, account.Id, AuditActions.AuthenticatorEnabled, detail: null, cancellationToken);

        // Setting up changes the security stamp; renew the cookie so this session stays valid.
        await signIn.RefreshSignInAsync(account);
        return TypedResults.Ok(new RecoveryCodes([.. codes!]));
    }

    /// <summary>
    /// A member turns their own authenticator off (D-071), confirming with a current code from the
    /// app or a recovery code. Staff can't: their MFA is mandatory (D-059), and only an admin can
    /// reset it. Wrong codes count towards lockout, as at login.
    /// </summary>
    private static async Task<Results<NoContent, UnauthorizedHttpResult, ProblemHttpResult>> TurnOffAsync(
        TurnOffAuthenticatorRequest request, ClaimsPrincipal user, UserManager<Account> users,
        SignInManager<Account> signIn, AuthenticatorRemoval removal, CancellationToken cancellationToken)
    {
        if (await users.GetUserAsync(user) is not { } account)
        {
            return TypedResults.Unauthorized();
        }

        if (await Authenticator.IsStaffAsync(users, account))
        {
            return TypedResults.Problem(
                title: "Staff accounts must keep the authenticator app.",
                detail: "Ask an administrator to reset it if you have lost it.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "staff-must-keep-authenticator");
        }

        if (!account.TwoFactorEnabled)
        {
            return TypedResults.Problem(
                title: "The authenticator app isn't on.",
                statusCode: StatusCodes.Status409Conflict,
                type: "authenticator-not-enabled");
        }

        var valid = request.IsRecoveryCode
            ? (await users.RedeemTwoFactorRecoveryCodeAsync(account, Authenticator.NormalizeRecoveryCode(request.Code))).Succeeded
            : await users.VerifyTwoFactorTokenAsync(
                account, users.Options.Tokens.AuthenticatorTokenProvider, Authenticator.NormalizeAppCode(request.Code));
        if (!valid)
        {
            await users.AccessFailedAsync(account);
            return TypedResults.Problem(
                title: "That code doesn't match.",
                detail: "Type the code your authenticator app shows now, or a recovery code.",
                statusCode: StatusCodes.Status400BadRequest,
                type: "invalid-code");
        }

        await removal.RemoveAsync(account, AuthenticatorRemoval.Reason.TurnedOffByMember, account.Id, cancellationToken);

        // Turning it off changes the security stamp; renew the cookie so this session stays valid.
        await signIn.RefreshSignInAsync(account);
        return TypedResults.NoContent();
    }
}
