using Testcontainers.PostgreSql;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// One throwaway PostgreSQL container for the whole test run (Testcontainers), plus the
/// test mailbox (<see cref="TestMailbox"/>).
/// The image matches the one Aspire runs locally, so tests and dev use the same version.
/// Needs Docker; CI's ubuntu runners have it.
/// </summary>
[TestClass]
public static class PostgresDatabase
{
    private static PostgreSqlContainer? _container;

    public static string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("The PostgreSQL container has not started.");

    [AssemblyInitialize]
    public static async Task StartAsync(TestContext _)
    {
        _container = new PostgreSqlBuilder("docker.io/library/postgres:18.6")
            .WithDatabase("fitnesscenter")
            .Build();

        // Both containers start at the same time; PostgreSQL alone takes most of the
        // fixture's time (about 12 s of 20 s locally), so the mailbox waits for nothing.
        await Task.WhenAll(_container.StartAsync(), TestMailbox.StartAsync());

        // Tests run in parallel and each one starts the API, which migrates at startup in
        // Development. Migrating once here, first, stops those startups racing each other.
        await using var factory = new ApiFactory();
        factory.CreateClient().Dispose();
    }

    [AssemblyCleanup]
    public static async Task StopAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        await TestMailbox.StopAsync();
    }
}
