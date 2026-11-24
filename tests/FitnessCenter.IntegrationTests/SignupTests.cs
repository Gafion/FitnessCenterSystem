using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace FitnessCenter.IntegrationTests;

[TestClass]
public sealed class SignupTests
{
    private const string Password = "correct horse battery";

    /// <summary>D-038: a password signup is logged in only after confirming the email.</summary>
    [TestMethod]
    public async Task Signup_creates_the_member_who_logs_in_after_confirming_the_email()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var terms = await GetTermsAsync(client);
        var email = SyntheticEmail();

        using var signup = await client.PostAsJsonAsync("/api/members/signup", Request(email, terms, startNow: true));

        Assert.AreEqual(HttpStatusCode.Created, signup.StatusCode, await signup.Content.ReadAsStringAsync());
        var body = await signup.Content.ReadFromJsonAsync<SignupResponse>();
        Assert.AreNotEqual(Guid.Empty, body?.MemberId);
        Assert.IsTrue(body?.EmailConfirmationRequired);

        using var meBefore = await client.GetAsync(new Uri("/api/identity/me", UriKind.Relative));
        Assert.AreEqual(HttpStatusCode.Unauthorized, meBefore.StatusCode, "logged in before confirming the email");

        using var confirm = await TestAccounts.ConfirmEmailAsync(client, email);
        Assert.AreEqual(HttpStatusCode.NoContent, confirm.StatusCode, await confirm.Content.ReadAsStringAsync());
        using var login = await TestAccounts.LogInAsync(client, email, Password);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode);

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

    /// <summary>D-052: the day before the 18th birthday (in Copenhagen) is refused and creates nothing.</summary>
    [TestMethod]
    public async Task Signup_under_18_is_refused_and_creates_nothing()
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new FixedClock(SignupInstant)));
        using var client = factory.CreateClient();
        var terms = await GetTermsAsync(client);
        var email = SyntheticEmail();

        using var signup = await client.PostAsJsonAsync(
            "/api/members/signup", Request(email, terms) with { DateOfBirth = new DateOnly(2008, 11, 25) });

        Assert.AreEqual(HttpStatusCode.BadRequest, signup.StatusCode);
        var body = await signup.Content.ReadAsStringAsync();
        Assert.Contains("dateOfBirth", body);
        Assert.Contains("18 or older", body);
        await AssertNoAccountAsync(client, email);
    }

    /// <summary>D-052: on the 18th birthday (in Copenhagen) the signup succeeds.</summary>
    [TestMethod]
    public async Task Signup_on_the_18th_birthday_succeeds()
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new FixedClock(SignupInstant)));
        using var client = factory.CreateClient();
        var terms = await GetTermsAsync(client);

        using var signup = await client.PostAsJsonAsync(
            "/api/members/signup", Request(SyntheticEmail(), terms) with { DateOfBirth = new DateOnly(2008, 11, 24) });

        Assert.AreEqual(HttpStatusCode.Created, signup.StatusCode, await signup.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Signup_without_a_date_of_birth_is_refused_and_creates_nothing()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var terms = await GetTermsAsync(client);
        var email = SyntheticEmail();

        using var signup = await client.PostAsJsonAsync(
            "/api/members/signup", Request(email, terms) with { DateOfBirth = null });

        Assert.AreEqual(HttpStatusCode.BadRequest, signup.StatusCode);
        Assert.Contains("dateOfBirth", await signup.Content.ReadAsStringAsync());
        await AssertNoAccountAsync(client, email);
    }

    /// <summary>24 November 2026, 00:30 in Copenhagen (UTC+1), while UTC is still 23 November.</summary>
    private static readonly Instant SignupInstant = Instant.FromUtc(2026, 11, 23, 23, 30);

    private static async Task AssertNoAccountAsync(HttpClient client, string email)
    {
        using var login = await client.PostAsJsonAsync("/api/identity/login", new { email, password = Password });
        Assert.AreEqual(HttpStatusCode.Unauthorized, login.StatusCode, "an account was left behind");
    }

    private static async Task<SignupTerms> GetTermsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<SignupTerms>("/api/members/signup/terms"))!;

    private static SignupRequest Request(string email, SignupTerms terms, bool startNow = false) =>
        new("Test", "Member", email, Password, AdultDateOfBirth, AcceptTerms: true, terms.TermsVersion, startNow, terms.EarlyStartVersion);

    /// <summary>A synthetic date of birth that is 18+ on any date the tests run (D-052).</summary>
    private static readonly DateOnly AdultDateOfBirth = new(1990, 1, 1);

    // Synthetic data only (CLAUDE.md): a unique address on the reserved example.com domain.
    private static string SyntheticEmail() => $"member-{Guid.NewGuid():N}@example.com";

    private sealed class FixedClock(Instant now) : IClock
    {
        public Instant GetCurrentInstant() => now;
    }

    private sealed class FailingClock : IClock
    {
        public Instant GetCurrentInstant() => throw new InvalidOperationException("Simulated failure after the account was created.");
    }

    private sealed record SignupTerms(string TermsVersion, string EarlyStartVersion);

    private sealed record SignupRequest(
        string FirstName, string LastName, string Email, string Password, DateOnly? DateOfBirth,
        bool AcceptTerms, string TermsVersion, bool StartNow, string EarlyStartVersion);

    private sealed record SignupResponse(Guid MemberId, bool EmailConfirmationRequired);

    private sealed record CurrentAccount(Guid AccountId, string Email, string FirstName, string LastName);
}
