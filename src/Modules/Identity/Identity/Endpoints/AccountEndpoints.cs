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
        group.MapPost("/login", LoginAsync).WithName("Login").Produces(StatusCodes.Status401Unauthorized);
        group.MapPost("/logout", LogoutAsync).WithName("Logout").RequireAuthorization();
        group.MapGet("/me", GetMeAsync).WithName("GetCurrentAccount").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    internal sealed record LoginRequest(string Email, string Password);

    internal sealed record CurrentAccount(Guid AccountId, string Email, string FirstName, string LastName, bool HasGoogleLogin);

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> LoginAsync(
        LoginRequest request, SignInManager<Account> signIn)
    {
        var result = await signIn.PasswordSignInAsync(
            request.Email, request.Password, isPersistent: true, lockoutOnFailure: true);

        // Same answer for unknown email, wrong password and lockout, so the response
        // doesn't reveal which emails have accounts.
        return result.Succeeded ? TypedResults.NoContent() : TypedResults.Unauthorized();
    }

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
