using System.Security.Claims;
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

        group.MapPost("/login", LoginAsync).WithName("Login");
        group.MapPost("/logout", LogoutAsync).WithName("Logout").RequireAuthorization();
        group.MapGet("/me", GetMe).WithName("GetCurrentAccount").RequireAuthorization();

        return endpoints;
    }

    internal sealed record LoginRequest(string Email, string Password);

    internal sealed record CurrentAccount(Guid AccountId, string Email);

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

    private static Ok<CurrentAccount> GetMe(ClaimsPrincipal user)
    {
        var id = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue(ClaimTypes.Name)!;
        return TypedResults.Ok(new CurrentAccount(id, email));
    }
}
