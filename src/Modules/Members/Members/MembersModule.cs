using FitnessCenter.Modules.Members.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.Modules.Members;

/// <summary>
/// Entry point of the Members module. The API host calls these two methods and
/// nothing else in this project; everything else stays <c>internal</c>.
/// </summary>
public static class MembersModule
{
    public static IServiceCollection AddMembersModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseNames.FitnessCenter)
            ?? throw new InvalidOperationException(
                $"Missing connection string '{DatabaseNames.FitnessCenter}'. Aspire sets it locally (D-009).");

        services.AddDbContext<MembersDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MembersDbContext.Schema)));

        services.AddHealthChecks().AddDbContextCheck<MembersDbContext>("members-db");

        return services;
    }

    public static IEndpointRouteBuilder MapMembersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
