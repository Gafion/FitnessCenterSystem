using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FitnessCenter.Modules.Identity.Application;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Staff accounts come from an admin's invite by email (D-058, FIT-75). Each test creates its
/// own synthetic staff accounts directly, so parallel tests don't share or race over them.
/// </summary>
[TestClass]
public sealed partial class StaffInviteTests
{
    // Synthetic passwords, only for these tests' in-memory API.
    private const string StaffPassword = "staff test password";
    private const string NewStaffPassword = "new staff password";
    private const string InviteSubject = "You're invited to join the staff";

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
    [DynamicData(nameof(AllRoles))]
    public async Task Invite_then_accept_then_log_in_as_staff_with_the_role(string role)
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var email = SyntheticStaffEmail();

        using var invite = await InviteAsync(admin, email, role);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());

        using var browser = Factory.CreateClient();
        using var accept = await AcceptAsync(browser, await InviteLinkForAsync(email));
        Assert.AreEqual(HttpStatusCode.NoContent, accept.StatusCode, await accept.Content.ReadAsStringAsync());

        using var login = await TestAccounts.LogInAsync(browser, email, NewStaffPassword);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, await login.Content.ReadAsStringAsync());
        Assert.IsTrue(await IsInRoleAsync(Factory, email, role), $"{email} should have role {role}");
        Assert.AreEqual(1, (await RolesOfAsync(Factory, email)).Count, "an invite gives exactly one role");
    }

    [TestMethod]
    public async Task Invited_staff_must_set_up_the_authenticator_before_using_staff_endpoints()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var email = SyntheticStaffEmail();
        using var invite = await InviteAsync(admin, email, StaffRoles.Admin);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());
        using var browser = Factory.CreateClient();
        using var accept = await AcceptAsync(browser, await InviteLinkForAsync(email));
        Assert.AreEqual(HttpStatusCode.NoContent, accept.StatusCode, await accept.Content.ReadAsStringAsync());
        using var login = await TestAccounts.LogInAsync(browser, email, NewStaffPassword);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, await login.Content.ReadAsStringAsync());

        using var before = await InviteAsync(browser, SyntheticStaffEmail(), StaffRoles.FrontDesk);
        Assert.AreEqual(HttpStatusCode.Forbidden, before.StatusCode, "no authenticator yet");

        await TestAuthenticator.SetUpAsync(browser);
        using var after = await InviteAsync(browser, SyntheticStaffEmail(), StaffRoles.FrontDesk);
        Assert.AreEqual(HttpStatusCode.Created, after.StatusCode, await after.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Expired_link_is_refused()
    {
        var clock = new MovableClock(Instant.FromUtc(2026, 11, 24, 12, 0));
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(clock));
        using var admin = await LoggedInStaffAsync(factory, StaffRoles.Admin);
        var email = SyntheticStaffEmail();
        using var invite = await InviteAsync(admin, email, StaffRoles.FrontDesk);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());

        clock.Now += StaffInvites.Lifetime;
        using var browser = factory.CreateClient();
        using var accept = await AcceptAsync(browser, await InviteLinkForAsync(email));

        Assert.AreEqual(HttpStatusCode.Gone, accept.StatusCode);
        Assert.Contains("invite-expired", await accept.Content.ReadAsStringAsync());
        using var login = await TestAccounts.LogInAsync(browser, email, NewStaffPassword);
        Assert.AreEqual(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [TestMethod]
    public async Task Link_still_works_just_before_it_expires()
    {
        var clock = new MovableClock(Instant.FromUtc(2026, 11, 24, 12, 0));
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(clock));
        using var admin = await LoggedInStaffAsync(factory, StaffRoles.Admin);
        var email = SyntheticStaffEmail();
        using var invite = await InviteAsync(admin, email, StaffRoles.Instructor);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());

        clock.Now += StaffInvites.Lifetime - Duration.FromSeconds(1);
        using var browser = factory.CreateClient();
        using var accept = await AcceptAsync(browser, await InviteLinkForAsync(email));

        Assert.AreEqual(HttpStatusCode.NoContent, accept.StatusCode, await accept.Content.ReadAsStringAsync());
    }

    [TestMethod]
    [DynamicData(nameof(NonAdminRoles))]
    public async Task Non_admin_staff_cannot_invite(string role)
    {
        using var staff = await LoggedInStaffAsync(Factory, role);
        var email = SyntheticStaffEmail();

        using var invite = await InviteAsync(staff, email, StaffRoles.Instructor);

        Assert.AreEqual(HttpStatusCode.Forbidden, invite.StatusCode);
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, InviteSubject));
    }

    [TestMethod]
    public async Task Member_cannot_invite()
    {
        using var member = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(member, TestAccounts.SyntheticEmail());

        using var invite = await InviteAsync(member, SyntheticStaffEmail(), StaffRoles.Admin);

        Assert.AreEqual(HttpStatusCode.Forbidden, invite.StatusCode);
    }

    [TestMethod]
    public async Task Invite_requires_login()
    {
        using var anonymous = Factory.CreateClient();

        using var invite = await InviteAsync(anonymous, SyntheticStaffEmail(), StaffRoles.Admin);

        Assert.AreEqual(HttpStatusCode.Unauthorized, invite.StatusCode);
    }

    [TestMethod]
    public async Task Used_link_cannot_be_used_again()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var email = SyntheticStaffEmail();
        using var invite = await InviteAsync(admin, email, StaffRoles.Manager);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());
        var link = await InviteLinkForAsync(email);
        using var browser = Factory.CreateClient();
        using var first = await AcceptAsync(browser, link);
        Assert.AreEqual(HttpStatusCode.NoContent, first.StatusCode, await first.Content.ReadAsStringAsync());

        using var second = await AcceptAsync(browser, link, password: "another password entirely");

        Assert.AreEqual(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("invite-used", await second.Content.ReadAsStringAsync());
        using var login = await TestAccounts.LogInAsync(browser, email, "another password entirely");
        Assert.AreEqual(HttpStatusCode.Unauthorized, login.StatusCode, "the second use must not change the password");
    }

    [TestMethod]
    public async Task Link_with_a_wrong_code_is_refused()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var email = SyntheticStaffEmail();
        using var invite = await InviteAsync(admin, email, StaffRoles.FrontDesk);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());
        var link = await InviteLinkForAsync(email);

        using var browser = Factory.CreateClient();
        using var accept = await AcceptAsync(browser, link with { Code = "bm90LWEtcmVhbC1jb2Rl" });

        Assert.AreEqual(HttpStatusCode.BadRequest, accept.StatusCode);
        Assert.Contains("invalid-invite", await accept.Content.ReadAsStringAsync());
        using var retry = await AcceptAsync(browser, link);
        Assert.AreEqual(HttpStatusCode.NoContent, retry.StatusCode, "a wrong code must not use up the invite");
    }

    [TestMethod]
    public async Task Invite_to_an_email_that_has_an_account_is_refused()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var memberEmail = TestAccounts.SyntheticEmail();
        using (var member = Factory.CreateClient())
        {
            await TestAccounts.SignUpAsync(member, memberEmail);
        }

        using var invite = await InviteAsync(admin, memberEmail, StaffRoles.FrontDesk);

        Assert.AreEqual(HttpStatusCode.Conflict, invite.StatusCode);
        Assert.Contains("account-exists", await invite.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Invite_with_an_unknown_role_is_refused()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var email = SyntheticStaffEmail();

        using var invite = await InviteAsync(admin, email, "Owner");

        Assert.AreEqual(HttpStatusCode.BadRequest, invite.StatusCode);
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, InviteSubject));
    }

    public static IEnumerable<object[]> AllRoles => StaffRoles.All.Select(role => new object[] { role });

    // Synthetic data only (CLAUDE.md): a unique address on the reserved example.com domain.
    private static string SyntheticStaffEmail() => $"staff-{Guid.NewGuid():N}@example.com";

    private static Task<HttpResponseMessage> InviteAsync(HttpClient client, string email, string role) =>
        client.PostAsJsonAsync("/api/identity/staff-invites", new { email, role });

    private static Task<HttpResponseMessage> AcceptAsync(
        HttpClient client, InviteLink link, string password = NewStaffPassword) =>
        client.PostAsJsonAsync("/api/identity/staff-invites/accept", new
        {
            inviteId = link.InviteId,
            code = link.Code,
            firstName = "Test",
            lastName = "Staff",
            password,
        });

    private static async Task<InviteLink> InviteLinkForAsync(string email)
    {
        var text = await TestMailbox.TextToAsync(email, InviteSubject);
        var match = InviteLinkPattern().Match(text);
        Assert.IsTrue(match.Success, $"No invite link in: {text}");
        var query = QueryHelpers.ParseQuery(new Uri(match.Value).Query);
        return new InviteLink(Guid.Parse(query["invite"]!), query["code"].ToString());
    }

    /// <summary>A confirmed staff account with <paramref name="role"/>, logged in on a new client.</summary>
    private static async Task<HttpClient> LoggedInStaffAsync(ApiFactory factory, string role)
    {
        var email = SyntheticStaffEmail();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
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
        }

        var client = factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(client, email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, await login.Content.ReadAsStringAsync());

        // Staff endpoints need the authenticator app set up first (D-013, D-059).
        await TestAuthenticator.SetUpAsync(client);
        return client;
    }

    private static async Task<IList<string>> RolesOfAsync(ApiFactory factory, string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        var account = await users.FindByEmailAsync(email) ?? throw new AssertFailedException($"No account {email}.");
        return await users.GetRolesAsync(account);
    }

    private static async Task<bool> IsInRoleAsync(ApiFactory factory, string email, string role) =>
        (await RolesOfAsync(factory, email)).Contains(role);

    [GeneratedRegex(@"\S+/accept-invite\?\S+")]
    private static partial Regex InviteLinkPattern();

    private sealed record InviteLink(Guid InviteId, string Code);

    private sealed class MovableClock(Instant now) : IClock
    {
        public Instant Now { get; set; } = now;

        public Instant GetCurrentInstant() => Now;
    }
}
