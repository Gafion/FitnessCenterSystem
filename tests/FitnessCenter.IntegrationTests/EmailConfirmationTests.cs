using System.Net;
using System.Net.Http.Json;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>D-038: an account can't log in until its email is confirmed.</summary>
[TestClass]
public sealed class EmailConfirmationTests
{
    [TestMethod]
    public async Task Unconfirmed_member_with_the_right_password_is_told_to_confirm()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var email = SyntheticEmail();
        await SignUpAsync(client, email);

        using var login = await LogInAsync(client, email);

        Assert.AreEqual(HttpStatusCode.Forbidden, login.StatusCode);
        Assert.Contains("email-not-confirmed", await login.Content.ReadAsStringAsync());
        using var me = await client.GetAsync(new Uri("/api/identity/me", UriKind.Relative));
        Assert.AreEqual(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    /// <summary>Without the password, an unconfirmed account looks like no account at all.</summary>
    [TestMethod]
    public async Task Unconfirmed_member_with_a_wrong_password_gets_the_usual_401()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var email = SyntheticEmail();
        await SignUpAsync(client, email);

        using var wrong = await LogInAsync(client, email, "wrong password");
        using var unknown = await LogInAsync(client, SyntheticEmail(), "wrong password");

        Assert.AreEqual(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.AreEqual(await unknown.Content.ReadAsStringAsync(), await wrong.Content.ReadAsStringAsync());
    }

    /// <summary>Wrong guesses against an unconfirmed account count towards lockout too.</summary>
    [TestMethod]
    public async Task Unconfirmed_member_is_locked_out_after_repeated_wrong_passwords()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var email = SyntheticEmail();
        await SignUpAsync(client, email);

        // Identity's default: locked out after 5 failures.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var wrong = await LogInAsync(client, email, "wrong password");
            Assert.AreEqual(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        using var right = await LogInAsync(client, email);
        Assert.AreEqual(HttpStatusCode.Unauthorized, right.StatusCode, "the right password still worked after 5 failures");
    }

    [TestMethod]
    public async Task Confirmation_link_with_a_wrong_code_is_refused()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var email = SyntheticEmail();
        await SignUpAsync(client, email);
        var link = await TestMailbox.ConfirmationLinkForAsync(email);
        var accountId = Guid.Parse(Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(link.Query)["account"]!);

        using var confirm = await client.PostAsJsonAsync(
            "/api/identity/confirm-email", new { accountId, code = "bm90LWEtcmVhbC10b2tlbg" });

        Assert.AreEqual(HttpStatusCode.BadRequest, confirm.StatusCode);
        using var login = await LogInAsync(client, email);
        Assert.AreEqual(HttpStatusCode.Forbidden, login.StatusCode);
    }

    [TestMethod]
    public async Task Resend_sends_a_link_that_works()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var email = SyntheticEmail();
        await SignUpAsync(client, email);
        await TestMailbox.ConfirmationLinkForAsync(email);

        await ResendAndConfirmEmailAsync(client, email);

        Assert.AreEqual(2, await TestMailbox.CountTo(email));
        using var login = await LogInAsync(client, email);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode);
    }

    [TestMethod]
    public async Task Resend_for_an_unknown_email_answers_the_same_and_sends_nothing()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var email = SyntheticEmail();

        using var resend = await client.PostAsJsonAsync("/api/identity/confirm-email/resend", new { email });

        Assert.AreEqual(HttpStatusCode.NoContent, resend.StatusCode);
        await Task.Delay(500);
        Assert.AreEqual(0, await TestMailbox.CountTo(email));
    }

    [TestMethod]
    public async Task Google_signup_needs_no_email_confirmation()
    {
        await using var factory = new ApiFactory(FakeGoogle.Register);
        var email = SyntheticEmail();
        using var client = FakeGoogle.CreateClient(factory, Guid.NewGuid().ToString("N"), email);
        using (await client.GetAsync(new Uri("/api/identity/external/google", UriKind.Relative)))
        {
        }

        using (await client.GetAsync(new Uri("/api/identity/external/google/callback", UriKind.Relative)))
        {
        }

        var terms = await client.GetFromJsonAsync<Terms>("/api/members/signup/terms");
        using var signup = await client.PostAsJsonAsync("/api/members/signup/google", new
        {
            firstName = "Test",
            lastName = "Googler",
            acceptTerms = true,
            termsVersion = terms!.TermsVersion,
            startNow = false,
            earlyStartVersion = terms.EarlyStartVersion,
        });

        Assert.AreEqual(HttpStatusCode.Created, signup.StatusCode, await signup.Content.ReadAsStringAsync());
        Assert.IsFalse((await signup.Content.ReadFromJsonAsync<SignupResponse>())?.EmailConfirmationRequired);
        using var me = await client.GetAsync(new Uri("/api/identity/me", UriKind.Relative));
        Assert.AreEqual(HttpStatusCode.OK, me.StatusCode);
        Assert.AreEqual(0, await TestMailbox.CountTo(email));
    }

    private sealed record Terms(string TermsVersion, string EarlyStartVersion);

    private sealed record SignupResponse(Guid MemberId, bool EmailConfirmationRequired);
}
