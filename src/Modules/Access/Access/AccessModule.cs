using FitnessCenter.Modules.Access.Application;
using FitnessCenter.Modules.Access.Contracts;
using FitnessCenter.Modules.Access.Endpoints;
using FitnessCenter.Modules.Access.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FitnessCenter.Modules.Access;

/// <summary>
/// Entry point of the Access module: access cards and check-in at the entrance (D-003),
/// with a simulated card reader in the MVP (D-021). The API host calls these methods and
/// nothing else in this project.
/// </summary>
public static class AccessModule
{
    public static IServiceCollection AddAccessModule(this IServiceCollection services)
    {
        // The shared, request-scoped connection lets one transaction span modules (D-015).
        services.AddDbContext<AccessDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlConnection>(), npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AccessDbContext.Schema).UseNodaTime()));

        services.AddScoped<IAccessCards, CardIssuer>();
        services.AddScoped<CheckInHandler>();

        services.AddHealthChecks().AddDbContextCheck<AccessDbContext>("access-db");

        return services;
    }

    public static IEndpointRouteBuilder MapAccessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return AccessEndpoints.MapAccessEndpoints(endpoints);
    }

    /// <summary>Applies this module's migrations. The host calls it at startup in Development.</summary>
    public static async Task MigrateAccessDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
