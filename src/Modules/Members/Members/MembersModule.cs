using FitnessCenter.Modules.Members.Application;
using FitnessCenter.Modules.Members.Contracts;
using FitnessCenter.Modules.Members.Endpoints;
using FitnessCenter.Modules.Members.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FitnessCenter.Modules.Members;

/// <summary>
/// Entry point of the Members module. The API host calls these methods and
/// nothing else in this project; everything else stays <c>internal</c>.
/// </summary>
public static class MembersModule
{
    public static IServiceCollection AddMembersModule(this IServiceCollection services)
    {
        // The shared, request-scoped connection lets one transaction span modules (D-015).
        services.AddDbContext<MembersDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlConnection>(), npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MembersDbContext.Schema).UseNodaTime()));

        services.AddScoped<SignupHandler>();
        services.AddScoped<IMemberDirectory, MemberDirectory>();

        services.AddHealthChecks().AddDbContextCheck<MembersDbContext>("members-db");

        return services;
    }

    public static IEndpointRouteBuilder MapMembersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapSignupEndpoints();
    }

    /// <summary>Applies this module's migrations. The host calls it at startup in Development.</summary>
    public static async Task MigrateMembersDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MembersDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
