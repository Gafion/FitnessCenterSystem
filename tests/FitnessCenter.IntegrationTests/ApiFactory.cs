using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FitnessCenter.IntegrationTests;

/// <summary>Starts the real API in memory, pointed at the test PostgreSQL container.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:fitnesscenter", PostgresDatabase.ConnectionString);
    }
}
