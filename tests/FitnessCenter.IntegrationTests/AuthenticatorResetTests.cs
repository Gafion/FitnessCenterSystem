using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Losing the authenticator app (AU12, D-071, FIT-111): admins reset a staff member's, never their
/// own; members turn their own off with a code. Both email the account in its language (D-065).
/// Synthetic accounts only.
/// </summary>
[TestClass]
public sealed class AuthenticatorResetTests
{
    // Synthetic password, only for these tests' in-memory API.
    private const string StaffPassword = "reset test password";

    private static ApiFactory? _factory;

    private static ApiFactory Factory => _factory ?? throw new InvalidOperationException("Not initialized.");

    public static IEnumerable<object[]> NonAdminRoles =>
        [[StaffRoles.Manager], [StaffRoles.FrontDesk], [StaffRoles.Instructor]];

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
    public async Task Admin_reset_turns_mfa_off_and_forces_setup_at_the_next_login()
    {
        using var admin = await StaffAsync(StaffRoles.Admin);
        using var staff = await StaffAsync(StaffRoles.Manager);
        Assert.IsTrue(await IsStaffEndpointAllowedAsync(staff.Client), "before the reset");

        using var reset = await ResetAsync(admin.Client, staff.Id);

        Assert.AreEqual(HttpStatusCode.NoContent, reset.StatusCode, await reset.Content.ReadAsStringAsync());
        Assert.IsFalse(await IsStaffEndpointAllowedAsync(staff.Client), "the old session can't use staff endpoints");
        Assert.AreEqual(0, await RecoveryCodesLeftAsync(staff.Id), "the recovery codes are removed");

        using var browser = Factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(browser, staff.Email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, "the password alone logs in, so setup can happen");
        var me = await browser.GetFromJsonAsync<Me>("/api/identity/me");
        Assert.IsTrue(me!.MustSetUpAuthenticator);
        Assert.IsFalse(me.TwoFactorEnabled);
        Assert.IsFalse(await IsStaffEndpointAllowedAsync(browser), "staff endpoints wait for the new setup");

        var (newKey, _) = await TestAuthenticator.SetUpAsync(browser);
        Assert.AreNotEqual(Normalize(staff.Key), Normalize(newKey), "the old key is gone");
        Assert.IsTrue(await IsStaffEndpointAllowedAsync(browser));

        using var later = Factory.CreateClient();
        using var password = await TestAccounts.LogInAsync(later, staff.Email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.Forbidden, password.StatusCode, "the new authenticator asks for a code again");
        using var oldCode = await TestAuthenticator.LogInWithCodeAsync(later, TestAuthenticator.Code(staff.Key));
        Assert.AreEqual(HttpStatusCode.Unauthorized, oldCode.StatusCode, "a code from the old key doesn't work");
    }

    [TestMethod]
    [DataRow(Language.Danish, "Din autentificeringsapp er nulstillet", "En administrator har nulstillet")]
    [DataRow(Language.English, "Your authenticator app was reset", "An administrator reset")]
    public async Task Admin_reset_emails_the_staff_member_in_their_language(Language language, string subject, string text)
    {
        using var admin = await StaffAsync(StaffRoles.Admin);
        using var staff = await StaffAsync(StaffRoles.Instructor, language);

        using var reset = await ResetAsync(admin.Client, staff.Id);

        Assert.AreEqual(HttpStatusCode.NoContent, reset.StatusCode, await reset.Content.ReadAsStringAsync());
        StringAssert.Contains(await TestMailbox.TextToAsync(staff.Email, subject), text);
    }

    [TestMethod]
    [DynamicData(nameof(NonAdminRoles))]
    public async Task Only_admins_may_reset(string role)
    {
        using var notAdmin = await StaffAsync(role);
        using var staff = await StaffAsync(StaffRoles.FrontDesk);

        using var reset = await ResetAsync(notAdmin.Client, staff.Id);
        using var list = await notAdmin.Client.GetAsync(new Uri("/api/identity/staff-accounts", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.Forbidden, reset.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.IsTrue(await TwoFactorEnabledAsync(staff.Id), "nothing changed");
    }

    [TestMethod]
    public async Task Members_may_not_reset()
    {
        using var member = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(member, TestAccounts.SyntheticEmail());
        using var staff = await StaffAsync(StaffRoles.FrontDesk);

        using var reset = await ResetAsync(member, staff.Id);

        Assert.AreEqual(HttpStatusCode.Forbidden, reset.StatusCode);
        Assert.IsTrue(await TwoFactorEnabledAsync(staff.Id));
    }

    [TestMethod]
    public async Task An_admin_cannot_reset_their_own()
    {
        using var admin = await StaffAsync(StaffRoles.Admin);

        using var reset = await ResetAsync(admin.Client, admin.Id);

        Assert.AreEqual(HttpStatusCode.Forbidden, reset.StatusCode);
        StringAssert.Contains(await reset.Content.ReadAsStringAsync(), "cannot-reset-own-authenticator");
        Assert.IsTrue(await TwoFactorEnabledAsync(admin.Id));
        Assert.AreEqual(10, await RecoveryCodesLeftAsync(admin.Id));
    }

    [TestMethod]
    public async Task Another_admin_may_reset_an_admin()
    {
        using var admin = await StaffAsync(StaffRoles.Admin);
        using var otherAdmin = await StaffAsync(StaffRoles.Admin);

        using var reset = await ResetAsync(admin.Client, otherAdmin.Id);

        Assert.AreEqual(HttpStatusCode.NoContent, reset.StatusCode, await reset.Content.ReadAsStringAsync());
        Assert.IsFalse(await TwoFactorEnabledAsync(otherAdmin.Id));
    }

    [TestMethod]
    public async Task Members_and_unknown_accounts_are_not_found()
    {
        using var admin = await StaffAsync(StaffRoles.Admin);
        using var member = Factory.CreateClient();
        var memberEmail = TestAccounts.SyntheticEmail();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(member, memberEmail);
        await TestAuthenticator.SetUpAsync(member);
        var memberId = await AccountIdAsync(memberEmail);

        using var memberReset = await ResetAsync(admin.Client, memberId);
        using var unknownReset = await ResetAsync(admin.Client, Guid.NewGuid());

        Assert.AreEqual(HttpStatusCode.NotFound, memberReset.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, unknownReset.StatusCode);
        Assert.IsTrue(await TwoFactorEnabledAsync(memberId), "a member turns their own off");
    }

    [TestMethod]
    public async Task Staff_without_the_authenticator_have_nothing_to_reset()
    {
        using var admin = await StaffAsync(StaffRoles.Admin);
        var (id, _) = await CreateStaffAsync(StaffRoles.FrontDesk, Language.Danish);

        using var reset = await ResetAsync(admin.Client, id);

        Assert.AreEqual(HttpStatusCode.Conflict, reset.StatusCode);
        StringAssert.Contains(await reset.Content.ReadAsStringAsync(), "authenticator-not-enabled");
    }

    [TestMethod]
    public async Task Admins_list_staff_with_roles_and_authenticator_state_but_no_members()
    {
        using var admin = await StaffAsync(StaffRoles.Admin);
        using var withApp = await StaffAsync(StaffRoles.Instructor);
        var (withoutApp, _) = await CreateStaffAsync(StaffRoles.FrontDesk, Language.Danish);
        var memberEmail = TestAccounts.SyntheticEmail();
        using var member = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(member, memberEmail);
        var memberId = await AccountIdAsync(memberEmail);

        var staff = (await admin.Client.GetFromJsonAsync<List<StaffAccount>>("/api/identity/staff-accounts"))!;

        var listedWithApp = staff.Single(account => account.Id == withApp.Id);
        Assert.IsTrue(listedWithApp.AuthenticatorEnabled);
        CollectionAssert.AreEqual(new[] { StaffRoles.Instructor }, listedWithApp.StaffRoles.ToArray());
        Assert.AreEqual(withApp.Email, listedWithApp.Email);
        Assert.AreEqual($"Test {StaffRoles.Instructor}", listedWithApp.DisplayName);
        Assert.IsFalse(staff.Single(account => account.Id == withoutApp).AuthenticatorEnabled);
        Assert.IsFalse(staff.Any(account => account.Id == memberId), "members are not listed");
    }

    [TestMethod]
    [DataRow("da", "Din autentificeringsapp er slået fra", "Autentificeringsappen er nu slået fra")]
    [DataRow("en", "Your authenticator app is turned off", "The authenticator app is now turned off")]
    public async Task Members_turn_mfa_off_with_a_code_and_are_emailed(string language, string subject, string text)
    {
        var email = TestAccounts.SyntheticEmail();
        using var client = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(client, email);
        using var setLanguage = await client.PutAsJsonAsync("/api/identity/me/language", new { language });
        Assert.AreEqual(HttpStatusCode.NoContent, setLanguage.StatusCode);
        var (key, _) = await TestAuthenticator.SetUpAsync(client);

        using var turnOff = await TurnOffAsync(client, TestAuthenticator.Code(key));

        Assert.AreEqual(HttpStatusCode.NoContent, turnOff.StatusCode, await turnOff.Content.ReadAsStringAsync());
        var me = await client.GetFromJsonAsync<Me>("/api/identity/me");
        Assert.IsFalse(me!.TwoFactorEnabled, "this session stays logged in, with MFA off");
        Assert.IsFalse(me.MustSetUpAuthenticator);
        Assert.AreEqual(0, await RecoveryCodesLeftAsync(await AccountIdAsync(email)));
        using var browser = Factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(browser, email);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, "the password alone logs in again");
        StringAssert.Contains(await TestMailbox.TextToAsync(email, subject), text);
    }

    [TestMethod]
    public async Task Members_may_turn_mfa_off_with_a_recovery_code()
    {
        var email = TestAccounts.SyntheticEmail();
        using var client = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(client, email);
        var (_, codes) = await TestAuthenticator.SetUpAsync(client);

        using var turnOff = await TurnOffAsync(client, codes[0], isRecoveryCode: true);

        Assert.AreEqual(HttpStatusCode.NoContent, turnOff.StatusCode, await turnOff.Content.ReadAsStringAsync());
        Assert.IsFalse(await TwoFactorEnabledAsync(await AccountIdAsync(email)));
    }

    [TestMethod]
    public async Task A_wrong_code_leaves_mfa_on()
    {
        var email = TestAccounts.SyntheticEmail();
        using var client = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(client, email);
        var (key, _) = await TestAuthenticator.SetUpAsync(client);

        using var wrongCode = await TurnOffAsync(client, TestAuthenticator.WrongCode(key));
        using var wrongRecovery = await TurnOffAsync(client, "AAAAA-BBBBB", isRecoveryCode: true);

        Assert.AreEqual(HttpStatusCode.BadRequest, wrongCode.StatusCode);
        StringAssert.Contains(await wrongCode.Content.ReadAsStringAsync(), "invalid-code");
        Assert.AreEqual(HttpStatusCode.BadRequest, wrongRecovery.StatusCode);
        var accountId = await AccountIdAsync(email);
        Assert.IsTrue(await TwoFactorEnabledAsync(accountId));
        Assert.AreEqual(10, await RecoveryCodesLeftAsync(accountId));
    }

    [TestMethod]
    public async Task Staff_cannot_turn_their_own_mfa_off()
    {
        using var staff = await StaffAsync(StaffRoles.Admin);

        using var turnOff = await TurnOffAsync(staff.Client, TestAuthenticator.Code(staff.Key));

        Assert.AreEqual(HttpStatusCode.Forbidden, turnOff.StatusCode);
        StringAssert.Contains(await turnOff.Content.ReadAsStringAsync(), "staff-must-keep-authenticator");
        Assert.IsTrue(await TwoFactorEnabledAsync(staff.Id));
    }

    [TestMethod]
    public async Task Turning_off_needs_mfa_on_and_a_login()
    {
        using var member = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(member, TestAccounts.SyntheticEmail());
        using var anonymous = Factory.CreateClient();

        using var notOn = await TurnOffAsync(member, "123456");
        using var loggedOut = await TurnOffAsync(anonymous, "123456");

        Assert.AreEqual(HttpStatusCode.Conflict, notOn.StatusCode);
        StringAssert.Contains(await notOn.Content.ReadAsStringAsync(), "authenticator-not-enabled");
        Assert.AreEqual(HttpStatusCode.Unauthorized, loggedOut.StatusCode);
    }

    [TestMethod]
    public async Task An_admin_reset_is_recorded_in_the_audit_trail()
    {
        using var admin = await StaffAsync(StaffRoles.Admin);
        using var staff = await StaffAsync(StaffRoles.FrontDesk);

        using var reset = await ResetAsync(admin.Client, staff.Id);

        Assert.AreEqual(HttpStatusCode.NoContent, reset.StatusCode, await reset.Content.ReadAsStringAsync());
        var entry = (await AuditAsync(admin.Client)).Single(
            e => e.SubjectAccountId == staff.Id && e.Action == "authenticator-reset-by-admin");
        Assert.AreEqual("authenticator-reset-by-admin", entry.Action);
        Assert.AreEqual(admin.Id, entry.ActorAccountId);
    }

    [TestMethod]
    public async Task A_member_turning_mfa_off_is_recorded_with_themselves_as_the_actor()
    {
        var email = TestAccounts.SyntheticEmail();
        using var client = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(client, email);
        var (key, _) = await TestAuthenticator.SetUpAsync(client);
        using var admin = await StaffAsync(StaffRoles.Admin);

        using var turnOff = await TurnOffAsync(client, TestAuthenticator.Code(key));

        Assert.AreEqual(HttpStatusCode.NoContent, turnOff.StatusCode, await turnOff.Content.ReadAsStringAsync());
        var memberId = await AccountIdAsync(email);
        var entry = (await AuditAsync(admin.Client)).Single(
            e => e.SubjectAccountId == memberId && e.Action == "authenticator-turned-off");
        Assert.AreEqual("authenticator-turned-off", entry.Action);
        Assert.AreEqual(memberId, entry.ActorAccountId);
    }

    [TestMethod]
    public async Task A_refused_reset_is_not_recorded()
    {
        using var admin = await StaffAsync(StaffRoles.Admin);

        using var own = await ResetAsync(admin.Client, admin.Id);

        Assert.AreEqual(HttpStatusCode.Forbidden, own.StatusCode);
        Assert.IsFalse((await AuditAsync(admin.Client)).Any(
            e => e.SubjectAccountId == admin.Id && e.Action == "authenticator-reset-by-admin"));
    }

    [TestMethod]
    [DynamicData(nameof(NonAdminRoles))]
    public async Task Only_admins_may_read_the_audit_trail(string role)
    {
        using var notAdmin = await StaffAsync(role);
        using var member = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(member, TestAccounts.SyntheticEmail());
        using var anonymous = Factory.CreateClient();

        using var staffRead = await notAdmin.Client.GetAsync(new Uri("/api/identity/audit-events", UriKind.Relative));
        using var memberRead = await member.GetAsync(new Uri("/api/identity/audit-events", UriKind.Relative));
        using var anonymousRead = await anonymous.GetAsync(new Uri("/api/identity/audit-events", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.Forbidden, staffRead.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, memberRead.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousRead.StatusCode);
    }

    private static async Task<IReadOnlyList<AuditEntry>> AuditAsync(HttpClient admin) =>
        await admin.GetFromJsonAsync<IReadOnlyList<AuditEntry>>("/api/identity/audit-events") ?? [];

    private static Task<HttpResponseMessage> ResetAsync(HttpClient client, Guid accountId) =>
        client.DeleteAsync(new Uri($"/api/identity/staff-accounts/{accountId}/authenticator", UriKind.Relative));

    private static Task<HttpResponseMessage> TurnOffAsync(HttpClient client, string code, bool isRecoveryCode = false) =>
        client.PostAsJsonAsync("/api/identity/authenticator/turn-off", new { code, isRecoveryCode });

    /// <summary>A staff endpoint behind the Manager policy.</summary>
    private static async Task<bool> IsStaffEndpointAllowedAsync(HttpClient client)
    {
        using var response = await client.GetAsync(new Uri("/api/identity/staff?role=Instructor", UriKind.Relative));
        return response.StatusCode == HttpStatusCode.OK;
    }

    private static string Normalize(string key) => key.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();

    /// <summary>A synthetic staff account straight in the database, without the authenticator.</summary>
    private static async Task<(Guid Id, string Email)> CreateStaffAsync(string role, Language language)
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
            PreferredLanguage = language,
        };
        Assert.IsTrue((await users.CreateAsync(account, StaffPassword)).Succeeded);
        Assert.IsTrue((await users.AddToRoleAsync(account, role)).Succeeded);
        return (account.Id, email);
    }

    /// <summary>A staff account with the authenticator set up, logged in on its client.</summary>
    private static async Task<Staff> StaffAsync(string role, Language language = Language.Danish)
    {
        var (id, email) = await CreateStaffAsync(role, language);
        var client = Factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(client, email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, await login.Content.ReadAsStringAsync());
        var (key, _) = await TestAuthenticator.SetUpAsync(client);
        return new Staff(client, id, email, key);
    }

    private static async Task<Guid> AccountIdAsync(string email)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        return (await users.FindByEmailAsync(email))!.Id;
    }

    private static async Task<bool> TwoFactorEnabledAsync(Guid accountId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        return (await users.FindByIdAsync(accountId.ToString()))!.TwoFactorEnabled;
    }

    private static async Task<int> RecoveryCodesLeftAsync(Guid accountId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        return await users.CountRecoveryCodesAsync((await users.FindByIdAsync(accountId.ToString()))!);
    }

    private sealed record Staff(HttpClient Client, Guid Id, string Email, string Key) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }

    private sealed record Me(bool TwoFactorEnabled, bool MustSetUpAuthenticator);

    private sealed record AuditEntry(Guid Id, string Action, Guid ActorAccountId, Guid SubjectAccountId);

    private sealed record StaffAccount(
        Guid Id, string DisplayName, string Email, IReadOnlyList<string> StaffRoles, bool AuthenticatorEnabled);
}
