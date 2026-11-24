using FitnessCenter.Modules.Members.Application;
using FitnessCenter.Modules.Members.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NodaTime;

namespace FitnessCenter.Modules.Members.Endpoints;

internal static class SignupEndpoints
{
    public static IEndpointRouteBuilder MapSignupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/members").WithTags("Members");

        group.MapGet("/signup/terms", GetTerms).WithName("GetSignupTerms");
        group.MapPost("/signup", SignUpAsync).WithName("SignUp");
        group.MapPost("/signup/google", SignUpWithGoogleAsync).WithName("SignUpWithGoogle");

        return endpoints;
    }

    internal sealed record SignupTerms(
        string MembershipName, long MonthlyPriceOre, string TermsVersion, string TermsText, string EarlyStartVersion, string EarlyStartText);

    internal sealed record SignupRequest(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        DateOnly? DateOfBirth,
        bool AcceptTerms,
        string TermsVersion,
        bool StartNow,
        string EarlyStartVersion);

    /// <summary>
    /// Finishes a signup that started with Google; the email comes from Google. The date of
    /// birth is required, as in the password signup (D-052, D-054).
    /// </summary>
    internal sealed record GoogleSignupRequest(
        string FirstName,
        string LastName,
        DateOnly? DateOfBirth,
        bool AcceptTerms,
        string TermsVersion,
        bool StartNow,
        string EarlyStartVersion);

    /// <param name="EmailConfirmationRequired">
    /// True after a password signup: the member must confirm their email before logging in (D-038).
    /// </param>
    internal sealed record SignupResponse(Guid MemberId, bool EmailConfirmationRequired);

    /// <summary>The texts the signup form shows; their versions are sent back with the signup.</summary>
    private static Ok<SignupTerms> GetTerms() => TypedResults.Ok(new SignupTerms(
        MembershipType.General.Name,
        MembershipType.General.MonthlyPrice.Ore,
        ConsentTexts.TermsVersion,
        ConsentTexts.Terms,
        ConsentTexts.EarlyStartVersion,
        ConsentTexts.EarlyStart));

    private static async Task<Results<Created<SignupResponse>, ValidationProblem>> SignUpAsync(
        SignupRequest request, SignupHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new SignupCommand(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password,
                request.DateOfBirth is { } born ? LocalDate.FromDateOnly(born) : null,
                request.AcceptTerms,
                request.TermsVersion,
                request.StartNow,
                request.EarlyStartVersion),
            cancellationToken);

        return ToResponse(result);
    }

    private static async Task<Results<Created<SignupResponse>, ValidationProblem>> SignUpWithGoogleAsync(
        GoogleSignupRequest request, SignupHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GoogleSignupCommand(
                request.FirstName,
                request.LastName,
                request.DateOfBirth is { } born ? LocalDate.FromDateOnly(born) : null,
                request.AcceptTerms,
                request.TermsVersion,
                request.StartNow,
                request.EarlyStartVersion),
            cancellationToken);

        return ToResponse(result);
    }

    private static Results<Created<SignupResponse>, ValidationProblem> ToResponse(SignupResult result) =>
        result.Succeeded
            ? TypedResults.Created((string?)null, new SignupResponse(result.MemberId!.Value.Value, !result.SignedIn))
            : TypedResults.ValidationProblem(result.Errors);
}
