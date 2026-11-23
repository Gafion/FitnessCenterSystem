using Microsoft.AspNetCore.Http.HttpResults;

namespace FitnessCenter.Api;

/// <summary>
/// Tells the frontend whether the simulated pages are on (D-041). Each module's simulation
/// endpoints check the same <c>Simulation:Enabled</c> setting and answer 404 when it is off.
/// </summary>
internal static class SimulationEndpoints
{
    internal sealed record SimulationSettings(bool Enabled);

    public static IEndpointRouteBuilder MapSimulationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/simulation", GetSettings).WithTags("Simulation").WithName("GetSimulationSettings");
        return endpoints;
    }

    private static Ok<SimulationSettings> GetSettings(IConfiguration configuration) =>
        TypedResults.Ok(new SimulationSettings(configuration.GetValue<bool>("Simulation:Enabled")));
}
