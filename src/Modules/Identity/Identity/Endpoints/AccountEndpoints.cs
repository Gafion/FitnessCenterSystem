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

internal static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/identity").WithTags("Identity");

        // UnauthorizedHttpResult doesn't describe itself in OpenAPI, so the 401s are declared here.
        group.MapPost("/login", LoginAsync).WithName("Login").Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        group.MapPost("/login/two-factor", LogInWithCodeAsync).WithName("LoginWithCode")
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapPost("/confirm-email", ConfirmEmailAsync).WithName("ConfirmEmail");
        group.MapPost("/confirm-email/resend", ResendEmailConfirmationAsync).WithName("ResendEmailConfirmation");
        group.MapPost("/logout", LogoutAsync).WithName("Logout").RequireAuthorization();
        group.MapGet("/me", GetMeAsync).WithName("GetCurrentAccount").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapPut("/me/language", SetLanguageAsync).WithName("SetPreferredLanguage").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    internal sealed record LoginRequest(string Email, string Password);

    /// <param name="Code">A code from the authenticator app, or a recovery code.</param>
    /// <param name="IsRecoveryCode">True when <paramref name="Code"/> is a recovery code.</param>
    internal sealed record LoginWithCodeRequest(string Code, bool IsRecoveryCode);

    internal sealed record ConfirmEmailRequest(Guid AccountId, string Code);

    internal sealed record ResendEmailConfirmationRequest(string Email);

    /// <param name="Language">"da" or "en" (D-065).</param>
    internal sealed record SetLanguageRequest(string Language);

    /// <param name="PreferredLanguage">The language of the account's emails: "da" or "en" (D-065).</param>
    /// <param name="TwoFactorEnabled">The authenticator app is set up (D-059).</param>
    /// <param name="MustSetUpAuthenticator">A staff account without the authenticator: staff endpoints refuse it until it is set up (D-013).</param>
    /// <param name="StaffRoles">The account's staff roles (D-049), empty for a member. The screens use it to show staff links; the API checks the roles itself.</param>
    internal sealed record CurrentAccount(
        Guid AccountId, string Email, string FirstName, string LastName, bool HasGoogleLogin,
        bool TwoFactorEnabled, bool MustSetUpAuthenticator, string PreferredLanguage,
        IReadOnlyList<string> StaffRoles);

    private static async Task<Results<NoContent, UnauthorizedHttpResult, ProblemHttpResult>> LoginAsync(
        LoginRequest request, SignInManager<Account> signIn, UserManager<Account> users)
    {
        var result = await signIn.PasswordSignInAsync(
            request.Email, request.Password, isPersistent: true, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return TypedResults.NoContent();
        }

        // The password was right and the account has the authenticator on (D-059). Identity has
        // set a short-lived cookie that remembers who is logging in; the code comes next.
        if (result.RequiresTwoFactor)
        {
            return TypedResults.Problem(
                title: "Enter the code from your authenticator app.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "two-factor-required");
        }

        // Identity refuses an unconfirmed account before it checks the password (or counts a
        // failure). Say "confirm your email" only to someone who knows the password, and
        // count wrong guesses towards lockout here. Everyone else gets the same 401, so the
        // response doesn't reveal which emails have accounts.
        if (result.IsNotAllowed
            && await users.FindByEmailAsync(request.Email) is { EmailConfirmed: false } account
            && !await users.IsLockedOutAsync(account))
        {
            if (!await users.CheckPasswordAsync(account, request.Password))
            {
                await users.AccessFailedAsync(account);
                return TypedResults.Unauthorized();
            }

            return TypedResults.Problem(
                title: "Confirm your email first.",
                detail: "Follow the link in the email we sent when you signed up.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "email-not-confirmed");
        }

        return TypedResults.Unauthorized();
    }

    /// <summary>
    /// The second login step (D-059). Needs the cookie the password step set. Wrong codes count
    /// towards lockout, as wrong passwords do. A recovery code works once.
    /// </summary>
    private static async Task<Results<NoContent, UnauthorizedHttpResult>> LogInWithCodeAsync(
        LoginWithCodeRequest request, SignInManager<Account> signIn)
    {
        var result = request.IsRecoveryCode
            ? await signIn.TwoFactorRecoveryCodeSignInAsync(Authenticator.NormalizeRecoveryCode(request.Code))
            : await signIn.TwoFactorAuthenticatorSignInAsync(
                Authenticator.NormalizeAppCode(request.Code), isPersistent: true, rememberClient: false);
        return result.Succeeded ? TypedResults.NoContent() : TypedResults.Unauthorized();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ConfirmEmailAsync(
        ConfirmEmailRequest request, UserManager<Account> users, AuditTrail audit, CancellationToken cancellationToken)
    {
        var account = await users.FindByIdAsync(request.AccountId.ToString());
        var token = EmailConfirmation.DecodeCode(request.Code);
        if (account is null || token is null)
        {
            return InvalidConfirmationLink();
        }

        if (account.EmailConfirmed)
        {
            return TypedResults.NoContent();
        }

        var result = await users.ConfirmEmailAsync(account, token);
        if (!result.Succeeded)
        {
            return InvalidConfirmationLink();
        }

        await audit.RecordAsync(account.Id, account.Id, AuditActions.EmailConfirmed, detail: null, cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Sends a new link when the account exists and isn't confirmed yet. Always answers 204,
    /// so it doesn't reveal which emails have accounts.
    /// </summary>
    private static async Task<NoContent> ResendEmailConfirmationAsync(
        ResendEmailConfirmationRequest request, UserManager<Account> users, IEmailSender<Account> emails,
        HttpContext context)
    {
        if (await users.FindByEmailAsync(request.Email) is { EmailConfirmed: false } account)
        {
            await EmailConfirmation.SendAsync(users, emails, account, context.Request);
        }

        return TypedResults.NoContent();
    }

    private static ProblemHttpResult InvalidConfirmationLink() =>
        TypedResults.Problem(
            title: "This confirmation link doesn't work.",
            detail: "It may be old or already replaced by a newer one. Ask for a new link.",
            statusCode: StatusCodes.Status400BadRequest,
            type: "invalid-confirmation-link");

    private static async Task<NoContent> LogoutAsync(SignInManager<Account> signIn)
    {
        await signIn.SignOutAsync();
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<CurrentAccount>, UnauthorizedHttpResult>> GetMeAsync(
        ClaimsPrincipal user, UserManager<Account> users)
    {
        var account = await users.GetUserAsync(user);
        if (account is null)
        {
            return TypedResults.Unauthorized();
        }

        var logins = await users.GetLoginsAsync(account);
        return TypedResults.Ok(new CurrentAccount(
            account.Id, account.Email!, account.FirstName, account.LastName,
            logins.Any(login => login.LoginProvider == ExternalLogins.Google),
            account.TwoFactorEnabled,
            !account.TwoFactorEnabled && await Authenticator.IsStaffAsync(users, account),
            account.PreferredLanguage.Code(),
            (await users.GetRolesAsync(account)).Where(Contracts.StaffRoles.All.Contains).ToList()));
    }

    /// <summary>Changes the language of the account's emails (D-065).</summary>
    private static async Task<Results<NoContent, ValidationProblem, UnauthorizedHttpResult>> SetLanguageAsync(
        SetLanguageRequest request, ClaimsPrincipal user, UserManager<Account> users, AuditTrail audit,
        CancellationToken cancellationToken)
    {
        var account = await users.GetUserAsync(user);
        if (account is null)
        {
            return TypedResults.Unauthorized();
        }

        if (request.Language is not ("da" or "en"))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["language"] = ["Language must be \"da\" or \"en\"."],
            });
        }

        account.PreferredLanguage = Languages.FromCode(request.Language)!.Value;
        var updated = await users.UpdateAsync(account);
        if (!updated.Succeeded)
        {
            throw new InvalidOperationException(
                $"Saving the language failed: {string.Join(", ", updated.Errors.Select(e => e.Code))}");
        }

        await audit.RecordAsync(account.Id, account.Id, AuditActions.LanguageChanged, detail: null, cancellationToken);
        return TypedResults.NoContent();
    }
}
