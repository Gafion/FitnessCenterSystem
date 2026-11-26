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
/// Staff accounts come from an invite by email (D-058, FIT-75). Managers invite too (D-063),
/// and an invite to an existing account adds the role to it (D-064, FIT-100). Each test creates its
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

    // Roles that may not invite at all (D-063): only admins and managers invite.
    public static IEnumerable<object[]> NonInvitingRoles => [[StaffRoles.FrontDesk], [StaffRoles.Instructor]];

    // The roles a manager may give (D-063), and the ones only an admin may give.
    public static IEnumerable<object[]> ManagerMayGive => [[StaffRoles.FrontDesk], [StaffRoles.Instructor]];

    public static IEnumerable<object[]> OnlyAdminMayGive => [[StaffRoles.Manager], [StaffRoles.Admin]];

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
    public async Task Accepting_an_invite_is_audited_with_the_inviter_as_the_one_granting_the_role()
    {
        var (admin, adminId) = await LoggedInStaffWithIdAsync(Factory, StaffRoles.Admin);
        using var adminClient = admin;
        var email = SyntheticStaffEmail();
        using var invite = await InviteAsync(admin, email, StaffRoles.Instructor);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());

        using var browser = Factory.CreateClient();
        using var accept = await AcceptAsync(browser, await InviteLinkForAsync(email));
        Assert.AreEqual(HttpStatusCode.NoContent, accept.StatusCode, await accept.Content.ReadAsStringAsync());

        var staffId = await AuditTrailTests.AccountIdAsync(Factory, email);
        var events = (await AuditTrailTests.AuditAsync(Factory)).Where(e => e.SubjectAccountId == staffId).ToList();
        var created = events.Single(e => e.Action == "account-created");
        Assert.AreEqual(staffId, created.ActorAccountId);
        Assert.AreEqual("staff-invite", created.Detail);
        var granted = events.Single(e => e.Action == "role-granted");
        Assert.AreEqual(adminId, granted.ActorAccountId, "the admin who sent the invite granted the role");
        Assert.AreEqual(StaffRoles.Instructor, granted.Detail);
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
    [DynamicData(nameof(NonInvitingRoles))]
    public async Task Front_desk_and_instructor_cannot_invite(string role)
    {
        using var staff = await LoggedInStaffAsync(Factory, role);
        var email = SyntheticStaffEmail();

        using var invite = await InviteAsync(staff, email, StaffRoles.Instructor);

        Assert.AreEqual(HttpStatusCode.Forbidden, invite.StatusCode);
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, InviteSubject));
    }

    [TestMethod]
    [DynamicData(nameof(ManagerMayGive))]
    public async Task Manager_invites_front_desk_and_instructor(string role)
    {
        using var manager = await LoggedInStaffAsync(Factory, StaffRoles.Manager);
        var email = SyntheticStaffEmail();

        using var invite = await InviteAsync(manager, email, role);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());

        using var browser = Factory.CreateClient();
        using var accept = await AcceptAsync(browser, await InviteLinkForAsync(email));
        Assert.AreEqual(HttpStatusCode.NoContent, accept.StatusCode, await accept.Content.ReadAsStringAsync());
        CollectionAssert.AreEqual(new[] { role }, (await RolesOfAsync(Factory, email)).ToArray());
    }

    [TestMethod]
    [DynamicData(nameof(OnlyAdminMayGive))]
    public async Task Manager_cannot_give_manager_or_admin(string role)
    {
        using var manager = await LoggedInStaffAsync(Factory, StaffRoles.Manager);
        var email = SyntheticStaffEmail();

        using var invite = await InviteAsync(manager, email, role);

        Assert.AreEqual(HttpStatusCode.Forbidden, invite.StatusCode);
        Assert.Contains("role-not-allowed", await invite.Content.ReadAsStringAsync());
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
    public async Task Member_accepts_an_invite_keeps_the_membership_and_gains_the_role()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var memberEmail = TestAccounts.SyntheticEmail();
        using var member = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(member, memberEmail);
        var membershipBefore = await MembershipAsync(member);

        using var invite = await InviteAsync(admin, memberEmail, StaffRoles.FrontDesk);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());
        var link = await InviteLinkForAsync(memberEmail);
        Assert.IsTrue(link.ForExistingAccount, "the link asks an existing account to log in and accept");

        using var accept = await AcceptForAccountAsync(member, link);
        Assert.AreEqual(HttpStatusCode.NoContent, accept.StatusCode, await accept.Content.ReadAsStringAsync());

        CollectionAssert.AreEqual(new[] { StaffRoles.FrontDesk }, (await RolesOfAsync(Factory, memberEmail)).ToArray());
        Assert.AreEqual(membershipBefore, await MembershipAsync(member), "the membership stays as it was");
        using var again = Factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(again, memberEmail);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, "the password stays as it was");
    }

    [TestMethod]
    public async Task Member_turned_staff_must_set_up_the_authenticator_before_using_staff_endpoints()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var memberEmail = TestAccounts.SyntheticEmail();
        using var member = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(member, memberEmail);
        using var invite = await InviteAsync(admin, memberEmail, StaffRoles.Manager);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());
        using var accept = await AcceptForAccountAsync(member, await InviteLinkForAsync(memberEmail));
        Assert.AreEqual(HttpStatusCode.NoContent, accept.StatusCode, await accept.Content.ReadAsStringAsync());

        // Inviting needs the Manager policy, so it shows whether the staff gate lets them in.
        using var before = await InviteAsync(member, SyntheticStaffEmail(), StaffRoles.FrontDesk);
        Assert.AreEqual(HttpStatusCode.Forbidden, before.StatusCode, "no authenticator yet");

        await TestAuthenticator.SetUpAsync(member);
        using var after = await InviteAsync(member, SyntheticStaffEmail(), StaffRoles.FrontDesk);
        Assert.AreEqual(HttpStatusCode.Created, after.StatusCode, await after.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Existing_account_cannot_accept_by_setting_a_password()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var memberEmail = TestAccounts.SyntheticEmail();
        using (var member = Factory.CreateClient())
        {
            await TestAccounts.SignUpAsync(member, memberEmail);
        }

        using var invite = await InviteAsync(admin, memberEmail, StaffRoles.FrontDesk);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());
        using var browser = Factory.CreateClient();
        using var accept = await AcceptAsync(browser, await InviteLinkForAsync(memberEmail));

        Assert.AreEqual(HttpStatusCode.Conflict, accept.StatusCode);
        Assert.Contains("account-exists", await accept.Content.ReadAsStringAsync());
        Assert.AreEqual(0, (await RolesOfAsync(Factory, memberEmail)).Count);
    }

    [TestMethod]
    public async Task Invite_cannot_be_accepted_by_another_account()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var invitedEmail = TestAccounts.SyntheticEmail();
        using (var invited = Factory.CreateClient())
        {
            await TestAccounts.SignUpAsync(invited, invitedEmail);
        }

        using var invite = await InviteAsync(admin, invitedEmail, StaffRoles.Instructor);
        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());
        var otherEmail = TestAccounts.SyntheticEmail();
        using var other = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(other, otherEmail);

        using var accept = await AcceptForAccountAsync(other, await InviteLinkForAsync(invitedEmail));

        Assert.AreEqual(HttpStatusCode.Forbidden, accept.StatusCode);
        Assert.Contains("invite-for-another-account", await accept.Content.ReadAsStringAsync());
        Assert.AreEqual(0, (await RolesOfAsync(Factory, otherEmail)).Count);
        Assert.AreEqual(0, (await RolesOfAsync(Factory, invitedEmail)).Count);
    }

    [TestMethod]
    public async Task Accepting_for_an_account_requires_login()
    {
        using var anonymous = Factory.CreateClient();

        using var accept = await AcceptForAccountAsync(anonymous, new InviteLink(Guid.NewGuid(), "code", true));

        Assert.AreEqual(HttpStatusCode.Unauthorized, accept.StatusCode);
    }

    [TestMethod]
    public async Task Invite_to_an_account_that_has_the_role_is_refused()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var email = SyntheticStaffEmail();
        using var first = await InviteAsync(admin, email, StaffRoles.Instructor);
        Assert.AreEqual(HttpStatusCode.Created, first.StatusCode, await first.Content.ReadAsStringAsync());
        using var browser = Factory.CreateClient();
        using var accept = await AcceptAsync(browser, await InviteLinkForAsync(email));
        Assert.AreEqual(HttpStatusCode.NoContent, accept.StatusCode, await accept.Content.ReadAsStringAsync());

        using var second = await InviteAsync(admin, email, StaffRoles.Instructor);

        Assert.AreEqual(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("already-has-role", await second.Content.ReadAsStringAsync());
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

    [TestMethod]
    public async Task Admin_may_give_every_role()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);

        var roles = await InvitableRolesAsync(admin);

        CollectionAssert.AreEqual(StaffRoles.All.ToArray(), roles);
    }

    [TestMethod]
    public async Task Manager_may_give_front_desk_and_instructor_only()
    {
        using var manager = await LoggedInStaffAsync(Factory, StaffRoles.Manager);

        var roles = await InvitableRolesAsync(manager);

        CollectionAssert.AreEqual(new[] { StaffRoles.FrontDesk, StaffRoles.Instructor }, roles);
    }

    [TestMethod]
    [DynamicData(nameof(NonInvitingRoles))]
    public async Task Front_desk_and_instructor_get_no_invitable_roles(string role)
    {
        using var staff = await LoggedInStaffAsync(Factory, role);

        using var response = await staff.GetAsync(new Uri("/api/identity/staff-invites/roles", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Invite_to_a_new_email_is_in_the_language_the_inviter_picked()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var email = SyntheticStaffEmail();

        // The admin's app is in English (ApiFactory), but the invite is to be Danish.
        using var invite = await admin.PostAsJsonAsync(
            "/api/identity/staff-invites", new { email, role = StaffRoles.Instructor, language = "da" });

        Assert.AreEqual(HttpStatusCode.Created, invite.StatusCode, await invite.Content.ReadAsStringAsync());
        StringAssert.Contains(await TestMailbox.TextToAsync(email, StaffInvites.InviteSubjectDanish), "Hej,");
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, InviteSubject));
    }

    [TestMethod]
    public async Task Invite_with_an_unknown_language_is_refused()
    {
        using var admin = await LoggedInStaffAsync(Factory, StaffRoles.Admin);
        var email = SyntheticStaffEmail();

        using var invite = await admin.PostAsJsonAsync(
            "/api/identity/staff-invites", new { email, role = StaffRoles.Instructor, language = "de" });

        Assert.AreEqual(HttpStatusCode.BadRequest, invite.StatusCode);
        Assert.Contains("language", await invite.Content.ReadAsStringAsync());
        Assert.AreEqual(0, await TestMailbox.CountTo(email));
    }

    [TestMethod]
    public async Task Current_account_lists_its_staff_roles()
    {
        using var manager = await LoggedInStaffAsync(Factory, StaffRoles.Manager);

        var me = await manager.GetFromJsonAsync<StaffRolesOfMe>(new Uri("/api/identity/me", UriKind.Relative));

        CollectionAssert.AreEqual(new[] { StaffRoles.Manager }, me!.StaffRoles.ToArray());
    }

    private static async Task<string[]> InvitableRolesAsync(HttpClient client)
    {
        using var response = await client.GetAsync(new Uri("/api/identity/staff-invites/roles", UriKind.Relative));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<InvitableRolesBody>();
        return body!.Roles.ToArray();
    }

    private sealed record InvitableRolesBody(IReadOnlyList<string> Roles);

    private sealed record StaffRolesOfMe(IReadOnlyList<string> StaffRoles);

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
        return new InviteLink(
            Guid.Parse(query["invite"]!), query["code"].ToString(), query.TryGetValue("account", out var account) && account == "existing");
    }

    private static Task<HttpResponseMessage> AcceptForAccountAsync(HttpClient client, InviteLink link) =>
        client.PostAsJsonAsync("/api/identity/staff-invites/accept-existing", new
        {
            inviteId = link.InviteId,
            code = link.Code,
        });

    private static async Task<string> MembershipAsync(HttpClient member)
    {
        using var response = await member.GetAsync(new Uri("/api/members/me", UriKind.Relative));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>A confirmed staff account with <paramref name="role"/>, logged in on a new client.</summary>
    internal static async Task<HttpClient> LoggedInStaffAsync(ApiFactory factory, string role) =>
        (await LoggedInStaffWithIdAsync(factory, role)).Client;

    /// <returns>A client logged in as a new staff account with the role, and the account's ID.</returns>
    internal static async Task<(HttpClient Client, Guid AccountId)> LoggedInStaffWithIdAsync(
        ApiFactory factory, string role, string firstName = "Test", string? lastName = null)
    {
        var email = SyntheticStaffEmail();
        Guid accountId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
            var account = new Account
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName ?? role,
            };
            Assert.IsTrue((await users.CreateAsync(account, StaffPassword)).Succeeded);
            Assert.IsTrue((await users.AddToRoleAsync(account, role)).Succeeded);
            accountId = account.Id;
        }

        var client = factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(client, email, StaffPassword);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, await login.Content.ReadAsStringAsync());

        // Staff endpoints need the authenticator app set up first (D-013, D-059).
        await TestAuthenticator.SetUpAsync(client);
        return (client, accountId);
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

    private sealed record InviteLink(Guid InviteId, string Code, bool ForExistingAccount = false);

    private sealed class MovableClock(Instant now) : IClock
    {
        public Instant Now { get; set; } = now;

        public Instant GetCurrentInstant() => Now;
    }
}
