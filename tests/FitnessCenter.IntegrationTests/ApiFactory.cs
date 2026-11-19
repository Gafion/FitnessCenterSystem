using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Starts the real API in memory, pointed at the test PostgreSQL container.
/// <paramref name="configureServices"/> lets a test swap a service, e.g. the clock.
/// </summary>
public sealed class ApiFactory(Action<IServiceCollection>? configureServices = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:fitnesscenter", PostgresDatabase.ConnectionString);

        if (configureServices is not null)
        {
            builder.ConfigureTestServices(configureServices);
        }
    }
}
