using FitnessCenter.Modules.Identity;
using FitnessCenter.Modules.Members;
using Npgsql;

namespace FitnessCenter.Api.Database;

internal static class DatabaseExtensions
{
    /// <summary>The one PostgreSQL database every module shares, one schema each (D-014).</summary>
    public const string ConnectionName = "fitnesscenter";

    /// <summary>
    /// One pooled data source, and one connection per request that every module's
    /// DbContext uses. Sharing the connection is what lets a single transaction
    /// span modules (D-015).
    /// </summary>
    public static IServiceCollection AddFitnessCenterDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException(
                $"Missing connection string '{ConnectionName}'. Aspire sets it locally (D-009).");

        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddScoped(provider => provider.GetRequiredService<NpgsqlDataSource>().CreateConnection());

        return services;
    }

    /// <summary>Applies every module's migrations in dependency order (D-014): Identity, then Members.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await services.MigrateIdentityDatabaseAsync(cancellationToken);
        await services.MigrateMembersDatabaseAsync(cancellationToken);
    }
}
