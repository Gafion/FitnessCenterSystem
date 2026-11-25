using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Staff listed by role for the instructor picker (FIT-105): Manager policy (D-062),
/// id and display name only. Synthetic accounts only.
/// </summary>
[TestClass]
public sealed class StaffListTests
{
    // Synthetic password, only for these tests' in-memory API.
    private const string Password = "staff list test password";

    private static ApiFactory? _factory;

    private static ApiFactory Factory => _factory ?? throw new InvalidOperationException("Not initialized.");

    public static IEnumerable<object[]> ListingRoles => [[StaffRoles.Admin], [StaffRoles.Manager]];

    public static IEnumerable<object[]> NonListingRoles => [[StaffRoles.FrontDesk], [StaffRoles.Instructor]];

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
    [DynamicData(nameof(ListingRoles))]
    public async Task Admin_and_manager_list_instructors_only(string listerRole)
    {
        var instructor = await CreateStaffAsync(StaffRoles.Instructor, "Ida", $"Instructor-{Guid.NewGuid():N}");
        var frontDesk = await CreateStaffAsync(StaffRoles.FrontDesk, "Frank", $"Desk-{Guid.NewGuid():N}");
        using var lister = await StaffInviteTests.LoggedInStaffAsync(Factory, listerRole);

        var staff = await ListAsync(lister, StaffRoles.Instructor);

        var listed = staff.Single(member => member.Id == instructor.Id);
        Assert.AreEqual($"{instructor.FirstName} {instructor.LastName}", listed.DisplayName);
        Assert.IsFalse(staff.Any(member => member.Id == frontDesk.Id), "a front desk account is not an instructor");
        foreach (var member in staff)
        {
            Assert.IsTrue(await IsInRoleAsync(member.Id, StaffRoles.Instructor), $"{member.Id} is not an instructor");
        }
    }

    [TestMethod]
    public async Task Role_filter_returns_the_asked_role()
    {
        var frontDesk = await CreateStaffAsync(StaffRoles.FrontDesk, "Frida", $"Desk-{Guid.NewGuid():N}");
        var instructor = await CreateStaffAsync(StaffRoles.Instructor, "Ivan", $"Instructor-{Guid.NewGuid():N}");
        using var manager = await StaffInviteTests.LoggedInStaffAsync(Factory, StaffRoles.Manager);

        var staff = await ListAsync(manager, StaffRoles.FrontDesk);

        Assert.IsTrue(staff.Any(member => member.Id == frontDesk.Id));
        Assert.IsFalse(staff.Any(member => member.Id == instructor.Id));
    }

    [TestMethod]
    public async Task Members_are_not_listed()
    {
        var member = await CreateAccountAsync("Mona", $"Member-{Guid.NewGuid():N}");
        using var manager = await StaffInviteTests.LoggedInStaffAsync(Factory, StaffRoles.Manager);

        foreach (var role in StaffRoles.All)
        {
            var staff = await ListAsync(manager, role);
            Assert.IsFalse(staff.Any(listed => listed.Id == member.Id), $"a member was listed under {role}");
        }
    }

    [TestMethod]
    public async Task Each_entry_has_id_and_display_name_only()
    {
        await CreateStaffAsync(StaffRoles.Instructor, "Ines", $"Instructor-{Guid.NewGuid():N}");
        using var manager = await StaffInviteTests.LoggedInStaffAsync(Factory, StaffRoles.Manager);

        using var response = await manager.GetAsync(Url(StaffRoles.Instructor));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.IsGreaterThan(0, json.RootElement.GetArrayLength());
        foreach (var entry in json.RootElement.EnumerateArray())
        {
            CollectionAssert.AreEquivalent(
                new[] { "id", "displayName" }, entry.EnumerateObject().Select(property => property.Name).ToArray());
        }
    }

    [TestMethod]
    [DynamicData(nameof(NonListingRoles))]
    public async Task Front_desk_and_instructor_are_forbidden(string role)
    {
        using var staff = await StaffInviteTests.LoggedInStaffAsync(Factory, role);

        using var response = await staff.GetAsync(Url(StaffRoles.Instructor));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Member_is_forbidden()
    {
        using var client = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(client, TestAccounts.SyntheticEmail());

        using var response = await client.GetAsync(Url(StaffRoles.Instructor));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Anonymous_is_unauthorized()
    {
        using var client = Factory.CreateClient();

        using var response = await client.GetAsync(Url(StaffRoles.Instructor));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    [DataRow("Member")]
    [DataRow("instructor")]
    [DataRow("")]
    public async Task Unknown_role_is_refused(string role)
    {
        using var manager = await StaffInviteTests.LoggedInStaffAsync(Factory, StaffRoles.Manager);

        using var response = await manager.GetAsync(Url(role));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Missing_role_is_refused()
    {
        using var manager = await StaffInviteTests.LoggedInStaffAsync(Factory, StaffRoles.Manager);

        using var response = await manager.GetAsync(new Uri("/api/identity/staff", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Uri Url(string role) =>
        new($"/api/identity/staff?role={Uri.EscapeDataString(role)}", UriKind.Relative);

    private static async Task<IReadOnlyList<StaffMemberBody>> ListAsync(HttpClient client, string role)
    {
        using var response = await client.GetAsync(Url(role));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<List<StaffMemberBody>>())!;
    }

    private static async Task<Account> CreateStaffAsync(string role, string firstName, string lastName)
    {
        var account = await CreateAccountAsync(firstName, lastName);
        await using var scope = Factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        var stored = await users.FindByIdAsync(account.Id.ToString()) ?? throw new AssertFailedException("No account.");
        Assert.IsTrue((await users.AddToRoleAsync(stored, role)).Succeeded);
        return account;
    }

    private static async Task<Account> CreateAccountAsync(string firstName, string lastName)
    {
        var email = $"staff-list-{Guid.NewGuid():N}@example.com";
        await using var scope = Factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        var account = new Account
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
        };
        Assert.IsTrue((await users.CreateAsync(account, Password)).Succeeded);
        return account;
    }

    private static async Task<bool> IsInRoleAsync(Guid accountId, string role)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        var account = await users.FindByIdAsync(accountId.ToString()) ?? throw new AssertFailedException("No account.");
        return await users.IsInRoleAsync(account, role);
    }

    private sealed record StaffMemberBody(Guid Id, string DisplayName);
}
