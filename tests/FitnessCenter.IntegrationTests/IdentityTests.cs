using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Identity.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.IntegrationTests;

[TestClass]
public sealed class IdentityTests
{
    private const string Password = "correct horse battery";

    [TestMethod]
    public async Task Account_can_log_in_and_see_itself()
    {
        await using var factory = new ApiFactory();
        var email = await CreateAccountAsync(factory, Password);
        using var client = factory.CreateClient();

        using var login = await client.PostAsJsonAsync("/api/identity/login", new { email, password = Password });
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode);

        var me = await client.GetFromJsonAsync<CurrentAccount>("/api/identity/me");
        Assert.AreEqual(email, me?.Email);
    }

    [TestMethod]
    public async Task Wrong_password_is_rejected()
    {
        await using var factory = new ApiFactory();
        var email = await CreateAccountAsync(factory, Password);
        using var client = factory.CreateClient();

        using var login = await client.PostAsJsonAsync("/api/identity/login", new { email, password = "wrong password" });

        Assert.AreEqual(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [TestMethod]
    public async Task Me_requires_login()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var me = await client.GetAsync(new Uri("/api/identity/me", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [TestMethod]
    public async Task Password_shorter_than_8_characters_is_refused()
    {
        await using var factory = new ApiFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();

        var result = await accounts.CreateLocalAccountAsync(
            new NewLocalAccount(SyntheticEmail(), "short", "Test", "Member"), CancellationToken.None);

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "PasswordTooShort"), string.Join(", ", result.Errors.Select(e => e.Code)));
    }

    [TestMethod]
    public async Task Taken_email_is_reported_once_as_an_email_error()
    {
        await using var factory = new ApiFactory();
        var email = await CreateAccountAsync(factory, "correct-horse");
        await using var scope = factory.Services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();

        var result = await accounts.CreateLocalAccountAsync(
            new NewLocalAccount(email, "correct-horse", "Test", "Member"), CancellationToken.None);

        Assert.IsFalse(result.Succeeded);
        CollectionAssert.AreEqual(new[] { "DuplicateEmail" }, result.Errors.Select(e => e.Code).ToArray());
    }

    private static async Task<string> CreateAccountAsync(ApiFactory factory, string password)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var email = SyntheticEmail();

        var result = await accounts.CreateLocalAccountAsync(
            new NewLocalAccount(email, password, "Test", "Member"), CancellationToken.None);
        Assert.IsTrue(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Code)));

        // D-038: accounts log in only after confirming their email.
        using var client = factory.CreateClient();
        await TestAccounts.ResendAndConfirmEmailAsync(client, email);
        return email;
    }

    // Synthetic data only (CLAUDE.md): a unique address on the reserved example.com domain.
    private static string SyntheticEmail() => $"member-{Guid.NewGuid():N}@example.com";

    private sealed record CurrentAccount(Guid AccountId, string Email);
}
