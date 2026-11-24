using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// MFA with an authenticator app (TOTP): mandatory for staff, optional for members (D-013,
/// D-059, FIT-74). Codes come from <see cref="TestAuthenticator"/>, a plain RFC 6238 generator
/// fed the setup key the API shows, as an app would be.
/// </summary>
[TestClass]
public sealed class AuthenticatorTests
{
    // Synthetic password, only for these tests' in-memory API.
    private const string StaffPassword = "staff test password";

    private static ApiFactory? _factory;

    private static ApiFactory Factory => _factory ?? throw new InvalidOperationException("Not initialized.");

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new ApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task Staff_without_the_authenticator_are_refused_staff_endpoints()
    {
        var email = await CreateStaffAsync(StaffRoles.Admin);
        using var client = Factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(client, email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, "the password alone logs in, so setup can happen");

        using var invite = await InviteAsync(client);
        Assert.AreEqual(HttpStatusCode.Forbidden, invite.StatusCode);
        var me = await client.GetFromJsonAsync<Me>("/api/identity/me");
        Assert.IsTrue(me!.MustSetUpAuthenticator);
        Assert.IsFalse(me.TwoFactorEnabled);
    }

    [TestMethod]
    public async Task Staff_with_the_authenticator_set_up_pass()
    {
        var email = await CreateStaffAsync(StaffRoles.Admin);
        using var client = Factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(client, email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode);

        await TestAuthenticator.SetUpAsync(client);

        using var invite = await InviteAsync(client);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());
        var me = await client.GetFromJsonAsync<Me>("/api/identity/me");
        Assert.IsFalse(me!.MustSetUpAuthenticator);
        Assert.IsTrue(me.TwoFactorEnabled);
    }

    [TestMethod]
    public async Task Staff_login_without_a_code_is_refused()
    {
        var (email, _, _) = await StaffWithAuthenticatorAsync();
        using var client = Factory.CreateClient();

        using var login = await TestAccounts.LogInAsync(client, email, StaffPassword);

        Assert.AreEqual(HttpStatusCode.Forbidden, login.StatusCode);
        StringAssert.Contains(await login.Content.ReadAsStringAsync(), "two-factor-required");
        using var me = await client.GetAsync(new Uri("/api/identity/me", UriKind.Relative));
        Assert.AreEqual(HttpStatusCode.Unauthorized, me.StatusCode, "no session before the code");
        using var invite = await InviteAsync(client);
        Assert.AreEqual(HttpStatusCode.Unauthorized, invite.StatusCode);
    }

    [TestMethod]
    public async Task Staff_login_with_the_correct_code_passes()
    {
        var (email, key, _) = await StaffWithAuthenticatorAsync();
        using var client = Factory.CreateClient();

        await TestAuthenticator.LogInAsync(client, email, StaffPassword, key);

        using var invite = await InviteAsync(client);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Staff_login_with_a_wrong_code_is_refused()
    {
        var (email, key, _) = await StaffWithAuthenticatorAsync();
        using var client = Factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(client, email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.Forbidden, login.StatusCode);

        using var code = await TestAuthenticator.LogInWithCodeAsync(client, TestAuthenticator.WrongCode(key));

        Assert.AreEqual(HttpStatusCode.Unauthorized, code.StatusCode);
        using var invite = await InviteAsync(client);
        Assert.AreEqual(HttpStatusCode.Unauthorized, invite.StatusCode);
    }

    [TestMethod]
    public async Task A_code_without_the_password_step_is_refused()
    {
        var (_, key, _) = await StaffWithAuthenticatorAsync();
        using var client = Factory.CreateClient();

        using var code = await TestAuthenticator.LogInWithCodeAsync(client, TestAuthenticator.Code(key));

        Assert.AreEqual(HttpStatusCode.Unauthorized, code.StatusCode);
    }

    [TestMethod]
    public async Task A_recovery_code_works_once()
    {
        var (email, _, recoveryCodes) = await StaffWithAuthenticatorAsync();
        Assert.HasCount(10, recoveryCodes);

        using var first = Factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(first, email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.Forbidden, login.StatusCode);
        using var recovery = await TestAuthenticator.LogInWithCodeAsync(first, recoveryCodes[0], isRecoveryCode: true);
        Assert.AreEqual(HttpStatusCode.NoContent, recovery.StatusCode, await recovery.Content.ReadAsStringAsync());
        using var invite = await InviteAsync(first);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode);

        using var second = Factory.CreateClient();
        using var again = await TestAccounts.LogInAsync(second, email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.Forbidden, again.StatusCode);
        using var reused = await TestAuthenticator.LogInWithCodeAsync(second, recoveryCodes[0], isRecoveryCode: true);
        Assert.AreEqual(HttpStatusCode.Unauthorized, reused.StatusCode, "a recovery code works only once");
    }

    [TestMethod]
    public async Task Setup_gives_a_standard_otpauth_uri_and_hides_the_key_once_on()
    {
        var email = await CreateStaffAsync(StaffRoles.FrontDesk);
        using var client = Factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(client, email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode);

        var setup = await client.GetFromJsonAsync<TestAuthenticator.Setup>("/api/identity/authenticator");
        Assert.IsFalse(setup!.Enabled);
        var uri = new Uri(setup.AuthenticatorUri!);
        Assert.AreEqual("otpauth", uri.Scheme);
        Assert.AreEqual("totp", uri.Host);
        var query = QueryHelpers.ParseQuery(uri.Query);
        Assert.AreEqual(setup.SharedKey!.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant(), query["secret"].ToString());
        Assert.AreEqual("FitnessCenter", query["issuer"].ToString());
        StringAssert.Contains(Uri.UnescapeDataString(uri.AbsolutePath), email);

        var again = await client.GetFromJsonAsync<TestAuthenticator.Setup>("/api/identity/authenticator");
        Assert.AreEqual(setup.SharedKey, again!.SharedKey, "reloading the page keeps the same key");

        await TestAuthenticator.SetUpAsync(client);
        var after = await client.GetFromJsonAsync<TestAuthenticator.Setup>("/api/identity/authenticator");
        Assert.IsTrue(after!.Enabled);
        Assert.IsNull(after.SharedKey);
        Assert.IsNull(after.AuthenticatorUri);
    }

    [TestMethod]
    public async Task Setup_with_a_wrong_code_is_refused_and_leaves_it_off()
    {
        var email = await CreateStaffAsync(StaffRoles.Instructor);
        using var client = Factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(client, email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode);
        var setup = await client.GetFromJsonAsync<TestAuthenticator.Setup>("/api/identity/authenticator");

        using var enable = await client.PostAsJsonAsync(
            "/api/identity/authenticator", new { code = TestAuthenticator.WrongCode(setup!.SharedKey!) });

        Assert.AreEqual(HttpStatusCode.BadRequest, enable.StatusCode);
        StringAssert.Contains(await enable.Content.ReadAsStringAsync(), "invalid-code");
        var me = await client.GetFromJsonAsync<Me>("/api/identity/me");
        Assert.IsFalse(me!.TwoFactorEnabled);
        Assert.IsTrue(me.MustSetUpAuthenticator);
    }

    [TestMethod]
    public async Task Members_log_in_without_a_code_and_are_not_asked_to_set_up()
    {
        using var client = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(client, TestAccounts.SyntheticEmail());

        var me = await client.GetFromJsonAsync<Me>("/api/identity/me");
        Assert.IsFalse(me!.MustSetUpAuthenticator);
        Assert.IsFalse(me.TwoFactorEnabled);
        using var membership = await client.GetAsync(new Uri("/api/members/me", UriKind.Relative));
        Assert.AreEqual(HttpStatusCode.OK, membership.StatusCode);
    }

    [TestMethod]
    public async Task Members_may_turn_on_the_authenticator_and_then_need_a_code()
    {
        var email = TestAccounts.SyntheticEmail();
        using var client = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(client, email);
        var (key, _) = await TestAuthenticator.SetUpAsync(client);

        using var browser = Factory.CreateClient();
        await TestAuthenticator.LogInAsync(browser, email, TestAccounts.Password, key);

        using var invite = await InviteAsync(browser);
        Assert.AreEqual(HttpStatusCode.Forbidden, invite.StatusCode, "the authenticator doesn't make a member staff");
    }

    private static Task<HttpResponseMessage> InviteAsync(HttpClient client) =>
        client.PostAsJsonAsync("/api/identity/staff-invites", new
        {
            email = $"staff-{Guid.NewGuid():N}@example.com",
            role = StaffRoles.FrontDesk,
        });

    /// <summary>A synthetic staff account straight in the database, without the authenticator.</summary>
    private static async Task<string> CreateStaffAsync(string role)
    {
        var email = $"staff-{Guid.NewGuid():N}@example.com";
        await using var scope = Factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        var account = new Account
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = role,
        };
        Assert.IsTrue((await users.CreateAsync(account, StaffPassword)).Succeeded);
        Assert.IsTrue((await users.AddToRoleAsync(account, role)).Succeeded);
        return email;
    }

    /// <summary>An admin who has set up the authenticator, then logged out.</summary>
    private static async Task<(string Email, string Key, IReadOnlyList<string> RecoveryCodes)> StaffWithAuthenticatorAsync()
    {
        var email = await CreateStaffAsync(StaffRoles.Admin);
        using var client = Factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(client, email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode);
        var (key, codes) = await TestAuthenticator.SetUpAsync(client);
        return (email, key, codes);
    }

    private sealed record Me(bool TwoFactorEnabled, bool MustSetUpAuthenticator);
}
