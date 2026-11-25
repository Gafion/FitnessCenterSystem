using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Identity;
using Npgsql;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Development staff with a configured authenticator key (FIT-109): the full-stack E2E tests
/// compute the login code from it. Runs on its own database, because StaffRoleTests seed the
/// same accounts and set up the authenticator themselves.
/// </summary>
[TestClass]
public sealed class DevelopmentStaffAuthenticatorTests
{
    // Synthetic test values, only for this test's in-memory API. Lower case with spaces, as the
    // setup page shows a key, to check the seed normalizes it.
    private const string StaffPassword = "staff test password";
    private const string Key = "jbsw y3dp ehpk 3pxp jbsw y3dp ehpk 3pxp";

    private static ApiFactory? _factory;

    private static ApiFactory Factory => _factory ?? throw new InvalidOperationException("Not initialized.");

    public static IEnumerable<object[]> Staff =>
    [
        ["dev-admin@example.com"],
        ["dev-manager@example.com"],
        ["dev-frontdesk@example.com"],
        ["dev-instructor@example.com"],
    ];

    [ClassInitialize]
    public static void Initialize(TestContext _)
    {
        _factory = NewFactory(Key);
        _factory.CreateClient().Dispose(); // starts the API, which migrates and seeds
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
    public async Task Seeded_staff_log_in_with_a_code_from_the_configured_key(string email)
    {
        using var client = Factory.CreateClient();

        await TestAuthenticator.LogInAsync(client, email, StaffPassword, Key);

        var setup = await client.GetFromJsonAsync<TestAuthenticator.Setup>("/api/identity/authenticator");
        Assert.IsTrue(setup?.Enabled, email);
    }

    [TestMethod]
    public async Task Seeding_again_keeps_the_key()
    {
        await Factory.Services.SeedDevelopmentStaffAsync(CancellationToken.None);

        using var client = Factory.CreateClient();
        await TestAuthenticator.LogInAsync(client, "dev-admin@example.com", StaffPassword, Key);
    }

    [TestMethod]
    public async Task A_wrong_code_is_refused()
    {
        using var client = Factory.CreateClient();
        using var login = await TestAccounts.LogInAsync(client, "dev-manager@example.com", StaffPassword);
        Assert.AreEqual(HttpStatusCode.Forbidden, login.StatusCode);

        using var code = await TestAuthenticator.LogInWithCodeAsync(client, TestAuthenticator.WrongCode(Key));
        Assert.AreNotEqual(HttpStatusCode.NoContent, code.StatusCode);
    }

    [TestMethod]
    public async Task A_key_that_is_not_Base32_stops_the_start()
    {
        await using var factory = NewFactory("not a base32 key 0189");

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => factory.CreateClient().Dispose());
        StringAssert.Contains(error.Message, "Identity:DevelopmentStaffAuthenticatorKey");
    }

    private static ApiFactory NewFactory(string key) => new(settings: new Dictionary<string, string?>
    {
        ["ConnectionStrings:fitnesscenter"] = new NpgsqlConnectionStringBuilder(PostgresDatabase.ConnectionString)
        {
            Database = "fitnesscenter_dev_staff_authenticator",
        }.ConnectionString,
        ["Identity:SeedDevelopmentStaff"] = "true",
        ["Identity:DevelopmentStaffPassword"] = StaffPassword,
        ["Identity:DevelopmentStaffAuthenticatorKey"] = key,
    });
}
