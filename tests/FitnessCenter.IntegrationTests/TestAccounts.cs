using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace FitnessCenter.IntegrationTests;

/// <summary>Signs up and confirms synthetic members through the API, as a browser would.</summary>
internal static class TestAccounts
{
    public const string Password = "correct horse battery";

    // Synthetic data only (CLAUDE.md): a unique address on the reserved example.com domain.
    public static string SyntheticEmail() => $"member-{Guid.NewGuid():N}@example.com";

    public static async Task SignUpAsync(HttpClient client, string email)
    {
        var terms = await client.GetFromJsonAsync<Terms>("/api/members/signup/terms");
        using var signup = await client.PostAsJsonAsync("/api/members/signup", new
        {
            firstName = "Test",
            lastName = "Member",
            email,
            password = Password,
            acceptTerms = true,
            termsVersion = terms!.TermsVersion,
            startNow = false,
            earlyStartVersion = terms.EarlyStartVersion,
        });
        Assert.AreEqual(HttpStatusCode.Created, signup.StatusCode, await signup.Content.ReadAsStringAsync());
    }

    /// <summary>Follows the newest confirmation link in the test mailbox (D-038).</summary>
    public static async Task<HttpResponseMessage> ConfirmEmailAsync(HttpClient client, string email)
    {
        var link = await TestMailbox.ConfirmationLinkForAsync(email);
        var query = QueryHelpers.ParseQuery(link.Query);
        return await client.PostAsJsonAsync("/api/identity/confirm-email", new
        {
            accountId = Guid.Parse(query["account"]!),
            code = query["code"].ToString(),
        });
    }

    /// <summary>Asks the API for a new confirmation link, then follows it.</summary>
    public static async Task ResendAndConfirmEmailAsync(HttpClient client, string email)
    {
        using var resend = await client.PostAsJsonAsync("/api/identity/confirm-email/resend", new { email });
        Assert.AreEqual(HttpStatusCode.NoContent, resend.StatusCode);
        using var confirm = await ConfirmEmailAsync(client, email);
        Assert.AreEqual(HttpStatusCode.NoContent, confirm.StatusCode, await confirm.Content.ReadAsStringAsync());
    }

    public static async Task<HttpResponseMessage> LogInAsync(HttpClient client, string email, string password = Password) =>
        await client.PostAsJsonAsync("/api/identity/login", new { email, password });

    /// <summary>Signup, confirmation and login: a member who is logged in on <paramref name="client"/>.</summary>
    public static async Task SignUpConfirmedAndLoggedInAsync(HttpClient client, string email)
    {
        await SignUpAsync(client, email);
        using var confirm = await ConfirmEmailAsync(client, email);
        Assert.AreEqual(HttpStatusCode.NoContent, confirm.StatusCode, await confirm.Content.ReadAsStringAsync());
        using var login = await LogInAsync(client, email);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, await login.Content.ReadAsStringAsync());
    }

    private sealed record Terms(string TermsVersion, string EarlyStartVersion);
}
