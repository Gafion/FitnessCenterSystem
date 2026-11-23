using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Stands in for Google in tests. Where Google would show its sign-in page and then call
/// back, this signs the given person into the external cookie at once and redirects to
/// where the API asked, as the real handler does after a successful Google login.
/// The person comes from request headers, so each test client can be a different person.
/// </summary>
internal static class FakeGoogle
{
    public const string SubjectHeader = "X-Fake-Google-Subject";
    public const string EmailHeader = "X-Fake-Google-Email";
    public const string EmailVerifiedHeader = "X-Fake-Google-Email-Verified";

    public static void Register(IServiceCollection services) =>
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, Handler>("Google", null);

    /// <summary>A client that follows no redirects, logged in to Google as a synthetic person.</summary>
    public static HttpClient CreateClient(ApiFactory factory, string subject, string email, bool emailVerified = true)
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(SubjectHeader, subject);
        client.DefaultRequestHeaders.Add(EmailHeader, email);
        client.DefaultRequestHeaders.Add(EmailVerifiedHeader, emailVerified ? "true" : "false");
        return client;
    }

    private sealed class Handler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            var headers = Request.Headers;
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, headers[SubjectHeader].ToString()),
                    new Claim(ClaimTypes.Email, headers[EmailHeader].ToString()),
                    new Claim("email_verified", headers[EmailVerifiedHeader].ToString()),
                    new Claim(ClaimTypes.GivenName, "Test"),
                    new Claim(ClaimTypes.Surname, "Googler"),
                ],
                Scheme.Name);

            await Context.SignInAsync(IdentityConstants.ExternalScheme, new ClaimsPrincipal(identity), properties);
            Response.Redirect(properties.RedirectUri!);
        }
    }
}
