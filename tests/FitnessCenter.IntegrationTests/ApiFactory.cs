using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Starts the real API in memory, pointed at the test PostgreSQL container.
/// <paramref name="configureServices"/> lets a test swap a service, e.g. the clock, and
/// <paramref name="settings"/> lets it change configuration, e.g. turn the simulation off.
/// </summary>
public sealed class ApiFactory(
    Action<IServiceCollection>? configureServices = null, IReadOnlyDictionary<string, string?>? settings = null)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:fitnesscenter", PostgresDatabase.ConnectionString);

        // Development loads the developer's user-secrets, which may hold a real Google
        // client. Tests never talk to Google; they register FakeGoogle when they need it.
        builder.UseSetting("Authentication:Google:ClientId", "");

        // Tests schedule the classes they need (BookingTests); the development schedule
        // would only add noise and read the clock at startup, which some tests make fail.
        builder.UseSetting("Booking:SeedDevelopmentSchedule", "false");

        // Development staff (D-049) come from the developer's user-secrets; StaffRoleTests
        // turn them on with their own synthetic password.
        builder.UseSetting("Identity:SeedDevelopmentStaff", "false");

        // The payment reminder job (D-042) would email every test's members on its own timer.
        // MissingPaymentTests run the reminders themselves, with a fixed clock.
        builder.UseSetting("Members:PaymentReminders:Enabled", "false");

        builder.UseSetting("Email:Smtp:Host", TestMailbox.SmtpHost);
        builder.UseSetting("Email:Smtp:Port", TestMailbox.SmtpMappedPort.ToString(CultureInfo.InvariantCulture));

        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
        {
            builder.UseSetting(key, value);
        }

        if (configureServices is not null)
        {
            builder.ConfigureTestServices(configureServices);
        }
    }
}
