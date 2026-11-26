using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Every account change is audited (D-072): who did it, to whom and what kind of change, and
/// never the changed value. Synthetic accounts only.
/// </summary>
[TestClass]
public sealed class AuditTrailTests
{
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
    public async Task A_member_signing_up_and_changing_their_account_is_audited_as_their_own_doing()
    {
        var email = TestAccounts.SyntheticEmail();
        using var client = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(client, email);
        using var language = await client.PutAsJsonAsync("/api/identity/me/language", new { language = "en" });
        Assert.AreEqual(HttpStatusCode.NoContent, language.StatusCode);
        await TestAuthenticator.SetUpAsync(client);

        var id = await AccountIdAsync(Factory, email);
        var events = (await AuditAsync(Factory)).Where(e => e.SubjectAccountId == id).ToList();

        CollectionAssert.AreEquivalent(
            new[] { "account-created", "email-confirmed", "language-changed", "authenticator-enabled" },
            events.Select(e => e.Action).ToArray());
        Assert.IsTrue(events.All(e => e.ActorAccountId == id), "the member made every change themselves");
        Assert.AreEqual("password", events.Single(e => e.Action == "account-created").Detail);
    }

    [TestMethod]
    public async Task The_audit_trail_holds_no_changed_values()
    {
        var email = TestAccounts.SyntheticEmail();
        using var client = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(client, email);
        using var language = await client.PutAsJsonAsync("/api/identity/me/language", new { language = "en" });

        var id = await AccountIdAsync(Factory, email);
        foreach (var audit in (await AuditAsync(Factory)).Where(e => e.SubjectAccountId == id))
        {
            Assert.IsTrue(
                audit.Detail is null or "password",
                $"{audit.Action} should carry no value, but has '{audit.Detail}'");
        }
    }

    [TestMethod]
    public async Task Admins_can_read_the_audit_trail()
    {
        var (admin, _) = await StaffInviteTests.LoggedInStaffWithIdAsync(Factory, StaffRoles.Admin);
        using var adminClient = admin;
        var email = TestAccounts.SyntheticEmail();
        using var member = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(member, email);

        var entries = await admin.GetFromJsonAsync<IReadOnlyList<Entry>>("/api/identity/audit-events");

        var id = await AccountIdAsync(Factory, email);
        Assert.IsTrue(entries!.Any(e => e.SubjectAccountId == id && e.Action == "account-created"));
    }

    internal static async Task<Guid> AccountIdAsync(ApiFactory factory, string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        return (await users.FindByEmailAsync(email) ?? throw new AssertFailedException($"No account {email}.")).Id;
    }

    internal static async Task<IReadOnlyList<AuditEvent>> AuditAsync(ApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.AuditEvents.AsNoTracking().ToListAsync();
    }

    private sealed record Entry(Guid SubjectAccountId, string Action);
}
