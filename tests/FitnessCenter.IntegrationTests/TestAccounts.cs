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

    /// <returns>The new member's id.</returns>
    public static async Task<Guid> SignUpAsync(HttpClient client, string email)
    {
        var terms = await client.GetFromJsonAsync<Terms>("/api/members/signup/terms");
        using var signup = await client.PostAsJsonAsync("/api/members/signup", new
        {
            firstName = "Test",
            lastName = "Member",
            email,
            password = Password,
            dateOfBirth = "1990-01-01", // synthetic, 18+ (D-052)
            acceptTerms = true,
            termsVersion = terms!.TermsVersion,
            startNow = false,
            earlyStartVersion = terms.EarlyStartVersion,
        });
        Assert.AreEqual(HttpStatusCode.Created, signup.StatusCode, await signup.Content.ReadAsStringAsync());
        return (await signup.Content.ReadFromJsonAsync<Signup>())!.MemberId;
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
    /// <returns>The member's id.</returns>
    /// <summary>
    /// Pays one month through the simulated payments API (D-041): the current month when
    /// <paramref name="periodFrom"/> is null. Check-in needs a payment (D-042, D-046).
    /// </summary>
    public static async Task PayAsync(HttpClient client, DateOnly? periodFrom = null)
    {
        using var response = await client.PostAsJsonAsync("/api/simulation/payments", new { periodFrom });
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    public static async Task<Guid> SignUpConfirmedAndLoggedInAsync(HttpClient client, string email)
    {
        var memberId = await SignUpAsync(client, email);
        using var confirm = await ConfirmEmailAsync(client, email);
        Assert.AreEqual(HttpStatusCode.NoContent, confirm.StatusCode, await confirm.Content.ReadAsStringAsync());
        using var login = await LogInAsync(client, email);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, await login.Content.ReadAsStringAsync());
        return memberId;
    }

    private sealed record Signup(Guid MemberId);

    private sealed record Terms(string TermsVersion, string EarlyStartVersion);
}
