using System.Net;
using FitnessCenter.Modules.Identity;
using FitnessCenter.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// The four staff roles and their policies (D-049, FIT-73). Staff come from the development
/// seed with a synthetic password; endpoints that require each policy are added for the test,
/// because no staff feature uses them yet.
/// </summary>
[TestClass]
public sealed class StaffRoleTests
{
    // Synthetic test password, only for this test's in-memory API.
    private const string StaffPassword = "staff test password";

    private static ApiFactory? _factory;
    private static string _memberEmail = "";

    private static readonly Dictionary<string, string> StaffKeys = [];

    private static ApiFactory Factory => _factory ?? throw new InvalidOperationException("Not initialized.");

    public static IEnumerable<object[]> Staff =>
    [
        [StaffRoles.Admin, "dev-admin@example.com", StaffPolicies.Admin],
        [StaffRoles.Manager, "dev-manager@example.com", StaffPolicies.Manager],
        [StaffRoles.FrontDesk, "dev-frontdesk@example.com", StaffPolicies.FrontDesk],
        [StaffRoles.Instructor, "dev-instructor@example.com", StaffPolicies.Instructor],
    ];

    public static IEnumerable<object[]> Policies => Staff.Select(s => new[] { s[2] }).Append([StaffPolicies.AnyStaff]);

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        // Only this class seeds staff, once, so parallel tests don't race to create them.
        _factory = new ApiFactory(
            services => services.AddTransient<IStartupFilter, PolicyProbe>(),
            new Dictionary<string, string?>
            {
                ["Identity:SeedDevelopmentStaff"] = "true",
                ["Identity:DevelopmentStaffPassword"] = StaffPassword,
            });

        // Staff endpoints need the authenticator app (D-013, D-059): set it up once per seeded
        // account; every login below then gives a code.
        foreach (var email in Staff.Select(s => (string)s[1]))
        {
            using var staff = _factory.CreateClient();
            using var login = await TestAccounts.LogInAsync(staff, email, StaffPassword);
            Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, await login.Content.ReadAsStringAsync());
            StaffKeys[email] = (await TestAuthenticator.SetUpAsync(staff)).Key;
        }

        using var client = _factory.CreateClient();
        _memberEmail = TestAccounts.SyntheticEmail();
        await TestAccounts.SignUpAsync(client, _memberEmail);
        using var confirm = await TestAccounts.ConfirmEmailAsync(client, _memberEmail);
        Assert.AreEqual(HttpStatusCode.NoContent, confirm.StatusCode, await confirm.Content.ReadAsStringAsync());
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [TestMethod]
    [DynamicData(nameof(Staff))]
    public async Task Role_policy_admits_its_role(string role, string email, string policy)
    {
        using var client = await LogInAsync(email, StaffPassword);

        Assert.AreEqual(HttpStatusCode.OK, await ProbeAsync(client, policy), role);
        Assert.AreEqual(HttpStatusCode.OK, await ProbeAsync(client, StaffPolicies.AnyStaff), role);
    }

    [TestMethod]
    [DynamicData(nameof(Staff))]
    public async Task Role_policy_refuses_the_other_staff_roles(string role, string email, string policy)
    {
        using var client = await LogInAsync(email, StaffPassword);

        foreach (var other in Staff.Select(s => (string)s[2]).Where(p => p != policy))
        {
            Assert.AreEqual(HttpStatusCode.Forbidden, await ProbeAsync(client, other), $"{role} on {other}");
        }
    }

    [TestMethod]
    [DynamicData(nameof(Policies))]
    public async Task Policy_refuses_a_member(string policy)
    {
        using var client = await LogInAsync(_memberEmail, TestAccounts.Password);

        Assert.AreEqual(HttpStatusCode.Forbidden, await ProbeAsync(client, policy));
    }

    [TestMethod]
    [DynamicData(nameof(Policies))]
    public async Task Policy_requires_login(string policy)
    {
        using var client = Factory.CreateClient();

        Assert.AreEqual(HttpStatusCode.Unauthorized, await ProbeAsync(client, policy));
    }

    [TestMethod]
    public async Task Seeding_development_staff_again_changes_nothing()
    {
        await Factory.Services.SeedDevelopmentStaffAsync(CancellationToken.None);

        using var client = await LogInAsync("dev-admin@example.com", StaffPassword);
        Assert.AreEqual(HttpStatusCode.OK, await ProbeAsync(client, StaffPolicies.Admin));
    }

    [TestMethod]
    public async Task Without_a_password_the_API_still_starts()
    {
        await using var factory = new ApiFactory(settings: new Dictionary<string, string?>
        {
            ["Identity:SeedDevelopmentStaff"] = "true",
            ["Identity:DevelopmentStaffPassword"] = "",
        });

        // Starting the API runs the seed; without a password it logs how to set one and skips.
        using var client = factory.CreateClient();
        using var health = await client.GetAsync(new Uri("/health", UriKind.Relative));
        Assert.AreEqual(HttpStatusCode.OK, health.StatusCode);
    }

    private static async Task<HttpClient> LogInAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        if (StaffKeys.TryGetValue(email, out var key))
        {
            await TestAuthenticator.LogInAsync(client, email, password, key);
            return client;
        }

        using var login = await TestAccounts.LogInAsync(client, email, password);
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode, $"{email}: {await login.Content.ReadAsStringAsync()}");
        return client;
    }

    private static async Task<HttpStatusCode> ProbeAsync(HttpClient client, string policy)
    {
        using var response = await client.GetAsync(new Uri($"/test/policy/{policy}", UriKind.Relative));
        return response.StatusCode;
    }

    /// <summary>One test-only endpoint per policy, behind the API's own pipeline.</summary>
    private sealed class PolicyProbe : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                foreach (var policy in Policies.Select(p => (string)p[0]))
                {
                    endpoints.MapGet($"/test/policy/{policy}", () => Results.Ok()).RequireAuthorization(policy);
                }
            });
        };
    }
}
