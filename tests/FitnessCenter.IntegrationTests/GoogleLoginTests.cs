using System.Net;
using System.Net.Http.Json;

namespace FitnessCenter.IntegrationTests;

/// <summary>Google login and signup (D-011, D-037), with <see cref="FakeGoogle"/> in place of Google.</summary>
[TestClass]
public sealed class GoogleLoginTests
{
    [TestMethod]
    public async Task Login_options_say_whether_Google_is_configured()
    {
        await using var withoutGoogle = new ApiFactory();
        await using var withGoogle = new ApiFactory(FakeGoogle.Register);

        Assert.IsFalse((await GetLoginOptionsAsync(withoutGoogle.CreateClient())).Google);
        Assert.IsTrue((await GetLoginOptionsAsync(withGoogle.CreateClient())).Google);
    }

    [TestMethod]
    public async Task New_person_finishes_signup_and_can_log_in_with_Google_again()
    {
        await using var factory = new ApiFactory(FakeGoogle.Register);
        var subject = NewSubject();
        var email = SyntheticEmail();
        using var client = FakeGoogle.CreateClient(factory, subject, email);

        Assert.AreEqual("/signup/google", await FollowGoogleLoginAsync(client));
        var pending = await client.GetFromJsonAsync<PendingExternalLogin>("/api/identity/external/pending");
        Assert.AreEqual(email, pending?.Email);
        Assert.AreEqual("Test", pending?.FirstName);

        var terms = await client.GetFromJsonAsync<SignupTerms>("/api/members/signup/terms");
        using var signup = await client.PostAsJsonAsync(
            "/api/members/signup/google",
            new GoogleSignupRequest("Test", "Googler", AcceptTerms: true, terms!.TermsVersion, StartNow: false, terms.EarlyStartVersion));
        Assert.AreEqual(HttpStatusCode.Created, signup.StatusCode, await signup.Content.ReadAsStringAsync());

        var me = await client.GetFromJsonAsync<CurrentAccount>("/api/identity/me");
        Assert.AreEqual(email, me?.Email);
        Assert.IsTrue(me?.HasGoogleLogin);

        // The pending Google login is used up once the member exists.
        using var pendingAfter = await client.GetAsync(new Uri("/api/identity/external/pending", UriKind.Relative));
        Assert.AreEqual(HttpStatusCode.Unauthorized, pendingAfter.StatusCode);

        // A fresh browser: the same Google account logs straight in.
        using var again = FakeGoogle.CreateClient(factory, subject, email);
        Assert.AreEqual("/account", await FollowGoogleLoginAsync(again));
        Assert.AreEqual(email, (await again.GetFromJsonAsync<CurrentAccount>("/api/identity/me"))?.Email);
    }

    [TestMethod]
    public async Task Google_signup_without_accepting_the_terms_creates_nothing()
    {
        await using var factory = new ApiFactory(FakeGoogle.Register);
        var subject = NewSubject();
        var email = SyntheticEmail();
        using var client = FakeGoogle.CreateClient(factory, subject, email);
        await FollowGoogleLoginAsync(client);
        var terms = await client.GetFromJsonAsync<SignupTerms>("/api/members/signup/terms");

        using var signup = await client.PostAsJsonAsync(
            "/api/members/signup/google",
            new GoogleSignupRequest("Test", "Googler", AcceptTerms: false, terms!.TermsVersion, StartNow: false, terms.EarlyStartVersion));

        Assert.AreEqual(HttpStatusCode.BadRequest, signup.StatusCode);
        using var again = FakeGoogle.CreateClient(factory, subject, email);
        Assert.AreEqual("/signup/google", await FollowGoogleLoginAsync(again), "an account was left behind");
    }

    [TestMethod]
    public async Task Google_signup_needs_a_pending_Google_login()
    {
        await using var factory = new ApiFactory(FakeGoogle.Register);
        using var client = factory.CreateClient();
        var terms = await client.GetFromJsonAsync<SignupTerms>("/api/members/signup/terms");

        using var signup = await client.PostAsJsonAsync(
            "/api/members/signup/google",
            new GoogleSignupRequest("Test", "Googler", AcceptTerms: true, terms!.TermsVersion, StartNow: false, terms.EarlyStartVersion));

        Assert.AreEqual(HttpStatusCode.BadRequest, signup.StatusCode);
        Assert.Contains("expired", await signup.Content.ReadAsStringAsync());
    }

    /// <summary>D-037: a matching email never links accounts; the person must log in first.</summary>
    [TestMethod]
    public async Task Google_email_of_an_existing_account_is_refused_not_linked()
    {
        await using var factory = new ApiFactory(FakeGoogle.Register);
        var email = SyntheticEmail();
        await SignUpWithPasswordAsync(factory, email);
        using var client = FakeGoogle.CreateClient(factory, NewSubject(), email);

        Assert.AreEqual("/login?error=google-email-taken", await FollowGoogleLoginAsync(client));
        using var me = await client.GetAsync(new Uri("/api/identity/me", UriKind.Relative));
        Assert.AreEqual(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [TestMethod]
    public async Task Unverified_Google_email_is_refused()
    {
        await using var factory = new ApiFactory(FakeGoogle.Register);
        using var client = FakeGoogle.CreateClient(factory, NewSubject(), SyntheticEmail(), emailVerified: false);

        Assert.AreEqual("/login?error=google-unverified", await FollowGoogleLoginAsync(client));
    }

    [TestMethod]
    public async Task Logged_in_member_connects_Google_and_then_logs_in_with_it()
    {
        await using var factory = new ApiFactory(FakeGoogle.Register);
        var email = SyntheticEmail();
        var subject = NewSubject();
        // The Google account may use another address than the member's login email.
        using var client = FakeGoogle.CreateClient(factory, subject, SyntheticEmail());
        await SignUpWithPasswordAsync(factory, email, client);

        Assert.AreEqual("/account", await FollowAsync(client, "/api/identity/external/google/link"));
        Assert.IsTrue((await client.GetFromJsonAsync<CurrentAccount>("/api/identity/me"))?.HasGoogleLogin);

        using var fresh = FakeGoogle.CreateClient(factory, subject, SyntheticEmail());
        Assert.AreEqual("/account", await FollowGoogleLoginAsync(fresh));
        Assert.AreEqual(email, (await fresh.GetFromJsonAsync<CurrentAccount>("/api/identity/me"))?.Email);
    }

    [TestMethod]
    public async Task Google_account_of_another_member_cannot_be_connected()
    {
        await using var factory = new ApiFactory(FakeGoogle.Register);
        var subject = NewSubject();
        using var first = FakeGoogle.CreateClient(factory, subject, SyntheticEmail());
        await SignUpWithPasswordAsync(factory, SyntheticEmail(), first);
        await FollowAsync(first, "/api/identity/external/google/link");

        using var second = FakeGoogle.CreateClient(factory, subject, SyntheticEmail());
        await SignUpWithPasswordAsync(factory, SyntheticEmail(), second);

        Assert.AreEqual("/account?error=google-in-use", await FollowAsync(second, "/api/identity/external/google/link"));
        Assert.IsFalse((await second.GetFromJsonAsync<CurrentAccount>("/api/identity/me"))?.HasGoogleLogin);
    }

    [TestMethod]
    public async Task Connecting_Google_requires_login()
    {
        await using var factory = new ApiFactory(FakeGoogle.Register);
        using var client = FakeGoogle.CreateClient(factory, NewSubject(), SyntheticEmail());

        using var link = await client.GetAsync(new Uri("/api/identity/external/google/link", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.Unauthorized, link.StatusCode);
    }

    private static Task<string> FollowGoogleLoginAsync(HttpClient client) =>
        FollowAsync(client, "/api/identity/external/google");

    /// <summary>Follows the API's redirects and returns the first frontend page it lands on.</summary>
    private static async Task<string> FollowAsync(HttpClient client, string path)
    {
        for (var hop = 0; hop < 5; hop++)
        {
            using var response = await client.GetAsync(new Uri(path, UriKind.Relative));
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, $"{path}: {await response.Content.ReadAsStringAsync()}");
            path = response.Headers.Location!.OriginalString;
            if (!path.StartsWith("/api/", StringComparison.Ordinal))
            {
                return path;
            }
        }

        throw new AssertFailedException("Too many redirects.");
    }

    /// <summary>A password member; with <paramref name="client"/>, also confirmed and logged in there (D-038).</summary>
    private static async Task SignUpWithPasswordAsync(ApiFactory factory, string email, HttpClient? client = null)
    {
        if (client is null)
        {
            using var anonymous = factory.CreateClient();
            await TestAccounts.SignUpAsync(anonymous, email);
            return;
        }

        await TestAccounts.SignUpConfirmedAndLoggedInAsync(client, email);
    }

    private static async Task<LoginOptions> GetLoginOptionsAsync(HttpClient client)
    {
        using (client)
        {
            return (await client.GetFromJsonAsync<LoginOptions>("/api/identity/login-options"))!;
        }
    }

    // Synthetic data only (CLAUDE.md).
    private static string SyntheticEmail() => $"member-{Guid.NewGuid():N}@example.com";

    private static string NewSubject() => Guid.NewGuid().ToString("N");

    private sealed record LoginOptions(bool Google);

    private sealed record PendingExternalLogin(string Email, string? FirstName, string? LastName);

    private sealed record SignupTerms(string TermsVersion, string EarlyStartVersion);

    private sealed record GoogleSignupRequest(
        string FirstName, string LastName, bool AcceptTerms, string TermsVersion, bool StartNow, string EarlyStartVersion);

    private sealed record CurrentAccount(string Email, bool HasGoogleLogin);
}
