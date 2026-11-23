using System.Security.Claims;
using FitnessCenter.Modules.Identity.Application;
using FitnessCenter.Modules.Identity.Infrastructure;
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
        group.MapPost("/confirm-email", ConfirmEmailAsync).WithName("ConfirmEmail");
        group.MapPost("/confirm-email/resend", ResendEmailConfirmationAsync).WithName("ResendEmailConfirmation");
        group.MapPost("/logout", LogoutAsync).WithName("Logout").RequireAuthorization();
        group.MapGet("/me", GetMeAsync).WithName("GetCurrentAccount").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    internal sealed record LoginRequest(string Email, string Password);

    internal sealed record ConfirmEmailRequest(Guid AccountId, string Code);

    internal sealed record ResendEmailConfirmationRequest(string Email);

    internal sealed record CurrentAccount(Guid AccountId, string Email, string FirstName, string LastName, bool HasGoogleLogin);

    private static async Task<Results<NoContent, UnauthorizedHttpResult, ProblemHttpResult>> LoginAsync(
        LoginRequest request, SignInManager<Account> signIn, UserManager<Account> users)
    {
        var result = await signIn.PasswordSignInAsync(
            request.Email, request.Password, isPersistent: true, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return TypedResults.NoContent();
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

    private static async Task<Results<NoContent, ProblemHttpResult>> ConfirmEmailAsync(
        ConfirmEmailRequest request, UserManager<Account> users)
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
        return result.Succeeded ? TypedResults.NoContent() : InvalidConfirmationLink();
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
            logins.Any(login => login.LoginProvider == ExternalLogins.Google)));
    }
}
