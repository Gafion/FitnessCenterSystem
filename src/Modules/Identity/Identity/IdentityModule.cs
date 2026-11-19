using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.Modules.Identity;

/// <summary>
/// Entry point of the Identity module. The API host calls these two methods and
/// nothing else in this project; everything else stays <c>internal</c>.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
