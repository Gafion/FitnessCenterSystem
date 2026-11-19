using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.Modules.Members;

/// <summary>
/// Entry point of the Members module. The API host calls these two methods and
/// nothing else in this project; everything else stays <c>internal</c>.
/// </summary>
public static class MembersModule
{
    public static IServiceCollection AddMembersModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapMembersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
