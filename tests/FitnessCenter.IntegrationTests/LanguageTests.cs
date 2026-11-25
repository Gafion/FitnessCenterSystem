using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Identity.Infrastructure;
using FitnessCenter.Modules.Members.Application;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Danish by default, English available (D-065, FIT-101): the signup texts follow
/// <c>Accept-Language</c>, and each account's emails are in its preferred language, which it
/// gets at signup and can change later.
/// </summary>
[TestClass]
public sealed class LanguageTests
{
    // Wednesday 10 March 2027, 10:30 in Copenhagen: a cancellation takes effect at the end of 30 April.
    private static readonly Instant Now = Instant.FromUtc(2027, 3, 10, 9, 30);

    private static ApiFactory Factory() => new(services => services.AddSingleton<IClock>(new FakeClock(Now)));

    /// <summary>A browser that sends no <c>Accept-Language</c>: the app's default, Danish.</summary>
    private static HttpClient DanishClient(ApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        return client;
    }

    [TestMethod]
    [DataRow(null, "terms-placeholder-v1-da", "Almindeligt medlemskab")]
    [DataRow("da", "terms-placeholder-v1-da", "Almindeligt medlemskab")]
    [DataRow("en", "terms-placeholder-v1", "General membership")]
    [DataRow("en-GB,en;q=0.9", "terms-placeholder-v1", "General membership")]
    [DataRow("de-DE,en;q=0.5,da;q=0.8", "terms-placeholder-v1-da", "Almindeligt medlemskab")]
    [DataRow("de-DE", "terms-placeholder-v1-da", "Almindeligt medlemskab")]
    public async Task Accept_Language_picks_the_language_of_the_signup_texts(
        string? acceptLanguage, string termsVersion, string membershipName)
    {
        await using var factory = new ApiFactory();
        using var client = DanishClient(factory);
        if (acceptLanguage is not null)
        {
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);
        }

        var terms = await client.GetFromJsonAsync<Terms>("/api/members/signup/terms");

        Assert.AreEqual(termsVersion, terms!.TermsVersion);
        Assert.AreEqual(membershipName, terms.MembershipName);
        StringAssert.StartsWith(terms.TermsText, termsVersion.EndsWith("-da", StringComparison.Ordinal) ? "PLADSHOLDER" : "PLACEHOLDER");
    }

    [TestMethod]
    public async Task A_Danish_account_gets_Danish_emails()
    {
        var email = SyntheticEmail();
        await using var factory = Factory();
        using var client = DanishClient(factory);
        await SignUpConfirmedAndLoggedInAsync(client, email);

        Assert.AreEqual(1, await TestMailbox.CountToAsync(email, SmtpEmailSender.ConfirmationSubjectDanish));
        Assert.AreEqual("da", (await client.GetFromJsonAsync<Me>("/api/identity/me"))!.PreferredLanguage);

        using var cancel = await client.PostAsync("/api/members/me/cancellation", null);
        Assert.AreEqual(HttpStatusCode.OK, cancel.StatusCode, await cancel.Content.ReadAsStringAsync());

        // Danish formats in the email (D-065): 30.04.2027.
        var text = await TestMailbox.TextToAsync(email, MembershipHandler.CancelledSubjectDanish);
        StringAssert.Contains(text, "Dit medlemskab slutter efter 30.04.2027.");
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, MembershipHandler.CancelledSubject));
    }

    [TestMethod]
    public async Task An_English_account_gets_English_emails()
    {
        var email = SyntheticEmail();
        await using var factory = Factory();
        using var client = factory.CreateClient(); // asks for English
        await SignUpConfirmedAndLoggedInAsync(client, email);

        Assert.AreEqual(1, await TestMailbox.CountToAsync(email, SmtpEmailSender.ConfirmationSubjectEnglish));
        Assert.AreEqual("en", (await client.GetFromJsonAsync<Me>("/api/identity/me"))!.PreferredLanguage);

        using var cancel = await client.PostAsync("/api/members/me/cancellation", null);
        Assert.AreEqual(HttpStatusCode.OK, cancel.StatusCode, await cancel.Content.ReadAsStringAsync());

        StringAssert.Contains(
            await TestMailbox.TextToAsync(email, MembershipHandler.CancelledSubject), "ends after 30 April 2027");
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, MembershipHandler.CancelledSubjectDanish));
    }

    [TestMethod]
    public async Task Changing_the_language_changes_the_language_of_later_emails()
    {
        var email = SyntheticEmail();
        await using var factory = Factory();
        using var client = DanishClient(factory);
        await SignUpConfirmedAndLoggedInAsync(client, email);
        using (var cancel = await client.PostAsync("/api/members/me/cancellation", null))
        {
            Assert.AreEqual(HttpStatusCode.OK, cancel.StatusCode);
        }

        using var change = await client.PutAsJsonAsync("/api/identity/me/language", new { language = "en" });
        Assert.AreEqual(HttpStatusCode.NoContent, change.StatusCode, await change.Content.ReadAsStringAsync());
        using var undo = await client.DeleteAsync("/api/members/me/cancellation");
        Assert.AreEqual(HttpStatusCode.OK, undo.StatusCode, await undo.Content.ReadAsStringAsync());

        Assert.AreEqual("en", (await client.GetFromJsonAsync<Me>("/api/identity/me"))!.PreferredLanguage);
        StringAssert.Contains(
            await TestMailbox.TextToAsync(email, MembershipHandler.UndoneSubject), "Your membership continues");
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, MembershipHandler.UndoneSubjectDanish));
    }

    [TestMethod]
    public async Task Only_Danish_and_English_can_be_chosen()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        using var change = await client.PutAsJsonAsync("/api/identity/me/language", new { language = "de" });

        Assert.AreEqual(HttpStatusCode.BadRequest, change.StatusCode);
        Assert.AreEqual("en", (await client.GetFromJsonAsync<Me>("/api/identity/me"))!.PreferredLanguage);
    }

    [TestMethod]
    public async Task Changing_the_language_needs_a_login()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();

        using var change = await client.PutAsJsonAsync("/api/identity/me/language", new { language = "en" });

        Assert.AreEqual(HttpStatusCode.Unauthorized, change.StatusCode);
    }

    private sealed class FakeClock(Instant now) : IClock
    {
        public Instant GetCurrentInstant() => now;
    }

    private sealed record Terms(string MembershipName, string TermsVersion, string TermsText);

    private sealed record Me(string PreferredLanguage);
}
