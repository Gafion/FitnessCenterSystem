using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace FitnessCenter.IntegrationTests;

[TestClass]
public sealed class SignupTests
{
    private const string Password = "correct horse battery";

    [TestMethod]
    public async Task Signup_creates_the_member_and_signs_them_in()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var terms = await GetTermsAsync(client);
        var email = SyntheticEmail();

        using var signup = await client.PostAsJsonAsync("/api/members/signup", Request(email, terms, startNow: true));

        Assert.AreEqual(HttpStatusCode.Created, signup.StatusCode, await signup.Content.ReadAsStringAsync());
        var body = await signup.Content.ReadFromJsonAsync<SignupResponse>();
        Assert.AreNotEqual(Guid.Empty, body?.MemberId);

        var me = await client.GetFromJsonAsync<CurrentAccount>("/api/identity/me");
        Assert.AreEqual(email, me?.Email);
        Assert.AreEqual("Test", me?.FirstName);
        Assert.AreEqual("Member", me?.LastName);
    }

    [TestMethod]
    public async Task Signup_without_accepting_the_terms_creates_nothing()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var terms = await GetTermsAsync(client);
        var email = SyntheticEmail();

        using var signup = await client.PostAsJsonAsync(
            "/api/members/signup", Request(email, terms) with { AcceptTerms = false });

        Assert.AreEqual(HttpStatusCode.BadRequest, signup.StatusCode);
        Assert.Contains("acceptTerms", await signup.Content.ReadAsStringAsync());
        await AssertNoAccountAsync(client, email);
    }

    [TestMethod]
    public async Task Signup_reports_consent_and_account_errors_together()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var terms = await GetTermsAsync(client);
        var email = SyntheticEmail();

        using var signup = await client.PostAsJsonAsync(
            "/api/members/signup", Request(email, terms) with { AcceptTerms = false, Password = "short" });

        Assert.AreEqual(HttpStatusCode.BadRequest, signup.StatusCode);
        var body = await signup.Content.ReadAsStringAsync();
        Assert.Contains("acceptTerms", body);
        Assert.Contains("password", body);
        await AssertNoAccountAsync(client, email);
    }

    [TestMethod]
    public async Task Signup_with_outdated_terms_is_refused()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var terms = await GetTermsAsync(client);

        using var signup = await client.PostAsJsonAsync(
            "/api/members/signup", Request(SyntheticEmail(), terms) with { TermsVersion = "terms-old" });

        Assert.AreEqual(HttpStatusCode.BadRequest, signup.StatusCode);
        Assert.Contains("termsVersion", await signup.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Same_email_cannot_sign_up_twice()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var terms = await GetTermsAsync(client);
        var email = SyntheticEmail();
        using var first = await client.PostAsJsonAsync("/api/members/signup", Request(email, terms));
        Assert.AreEqual(HttpStatusCode.Created, first.StatusCode);

        using var second = await client.PostAsJsonAsync("/api/members/signup", Request(email, terms));

        Assert.AreEqual(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Contains("email", await second.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// D-015/D-034: the account and the member are saved in one transaction. The clock is
    /// read after the account is created, so a failing clock proves the account is rolled back.
    /// </summary>
    [TestMethod]
    public async Task Failure_after_the_account_is_created_leaves_no_account_behind()
    {
        await using var failing = new ApiFactory(services => services.AddSingleton<IClock, FailingClock>());
        using var failingClient = failing.CreateClient();
        var terms = await GetTermsAsync(failingClient);
        var email = SyntheticEmail();

        using var signup = await failingClient.PostAsJsonAsync("/api/members/signup", Request(email, terms));
        Assert.AreEqual(HttpStatusCode.InternalServerError, signup.StatusCode);

        await AssertNoAccountAsync(failingClient, email);
    }

    private static async Task AssertNoAccountAsync(HttpClient client, string email)
    {
        using var login = await client.PostAsJsonAsync("/api/identity/login", new { email, password = Password });
        Assert.AreEqual(HttpStatusCode.Unauthorized, login.StatusCode, "an account was left behind");
    }

    private static async Task<SignupTerms> GetTermsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<SignupTerms>("/api/members/signup/terms"))!;

    private static SignupRequest Request(string email, SignupTerms terms, bool startNow = false) =>
        new("Test", "Member", email, Password, AcceptTerms: true, terms.TermsVersion, startNow, terms.EarlyStartVersion);

    // Synthetic data only (CLAUDE.md): a unique address on the reserved example.com domain.
    private static string SyntheticEmail() => $"member-{Guid.NewGuid():N}@example.com";

    private sealed class FailingClock : IClock
    {
        public Instant GetCurrentInstant() => throw new InvalidOperationException("Simulated failure after the account was created.");
    }

    private sealed record SignupTerms(string TermsVersion, string EarlyStartVersion);

    private sealed record SignupRequest(
        string FirstName, string LastName, string Email, string Password,
        bool AcceptTerms, string TermsVersion, bool StartNow, string EarlyStartVersion);

    private sealed record SignupResponse(Guid MemberId);

    private sealed record CurrentAccount(Guid AccountId, string Email, string FirstName, string LastName);
}
