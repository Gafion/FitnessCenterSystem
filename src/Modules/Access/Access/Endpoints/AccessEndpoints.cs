using System.Security.Claims;
using System.Text.Json.Serialization;
using FitnessCenter.Modules.Access.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.Modules.Access.Endpoints;

internal static class AccessEndpoints
{
    /// <summary>True turns the simulated card reader on (D-021, D-041). Off unless set.</summary>
    public const string SimulationSetting = "Simulation:Enabled";

    public static IEndpointRouteBuilder MapAccessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/access").WithTags("Access");

        group.MapGet("/cards/mine", GetMyCardAsync).WithName("GetMyCard").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);

        // Any logged-in account may use the simulated reader, but only while the setting is on (D-041).
        // The setting is checked before the login, so with it off the reader doesn't exist for anyone.
        // There is no endpoint for a real reader yet: how a reader proves who it is waits for the
        // hardware (FIT-33). It will call the same CheckInHandler.
        var reader = group.MapGroup("/simulation/reader")
            .AddEndpointFilter(SimulationOnAndLoggedIn);
        SimulationResponses(reader.MapGet("", () => TypedResults.NoContent()).WithName("GetSimulatedReader"));
        SimulationResponses(reader.MapPost("/swipes", SwipeAsync).WithName("SwipeCard"));
        SimulationResponses(reader.MapPost("/swipes/my-card", SwipeMyCardAsync).WithName("SwipeMyCard"));

        return endpoints;
    }

    /// <summary>404 when the simulation is off.</summary>
    private static void SimulationResponses(RouteHandlerBuilder endpoint) =>
        endpoint.Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status404NotFound);

    internal sealed record MyCard(string Uid);

    internal sealed record SwipeRequest(string CardUid);

    internal enum DoorOutcome
    {
        [JsonStringEnumMemberName("welcome")]
        Welcome,

        [JsonStringEnumMemberName("unknown-card")]
        UnknownCard,
    }

    /// <summary>What the door screen shows.</summary>
    /// <param name="FirstName">Only set on <c>welcome</c>.</param>
    internal sealed record SwipeResponse(
        [property: JsonConverter(typeof(JsonStringEnumConverter<DoorOutcome>))] DoorOutcome Outcome,
        string? FirstName,
        DateTimeOffset At);

    /// <summary>404 when the account isn't a member, or the member has no card.</summary>
    private static async Task<Results<Ok<MyCard>, NotFound>> GetMyCardAsync(
        ClaimsPrincipal user, CheckInHandler handler, CancellationToken cancellationToken)
    {
        var uid = await handler.FindCardUidAsync(AccountIdOf(user), cancellationToken);
        return uid is null ? TypedResults.NotFound() : TypedResults.Ok(new MyCard(uid));
    }

    /// <summary>Swipes any UID, as if someone held that card to the reader.</summary>
    private static async Task<Ok<SwipeResponse>> SwipeAsync(
        SwipeRequest request, CheckInHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(ToResponse(await handler.CheckInAsync(request.CardUid, cancellationToken)));

    /// <summary>Swipes the logged-in member's own card. 404 when there is no card.</summary>
    private static async Task<Results<Ok<SwipeResponse>, NotFound>> SwipeMyCardAsync(
        ClaimsPrincipal user, CheckInHandler handler, CancellationToken cancellationToken)
    {
        var uid = await handler.FindCardUidAsync(AccountIdOf(user), cancellationToken);
        return uid is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToResponse(await handler.CheckInAsync(uid, cancellationToken)));
    }

    private static SwipeResponse ToResponse(CheckInResult result) => new(
        result.Outcome switch
        {
            CheckInOutcome.Welcome => DoorOutcome.Welcome,
            CheckInOutcome.UnknownCard => DoorOutcome.UnknownCard,
            _ => throw new InvalidOperationException($"Unknown outcome {result.Outcome}."),
        },
        result.FirstName,
        result.At.ToDateTimeOffset());

    /// <summary>D-041: 404 while the simulation is off, then 401 without a login.</summary>
    private static async ValueTask<object?> SimulationOnAndLoggedIn(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        if (!http.RequestServices.GetRequiredService<IConfiguration>().GetValue(SimulationSetting, defaultValue: false))
        {
            return TypedResults.NotFound();
        }

        return http.User.Identity?.IsAuthenticated == true ? await next(context) : TypedResults.Unauthorized();
    }

    /// <summary>The logged-in account (the Identity cookie's user id). Only called once a login is required.</summary>
    private static Guid AccountIdOf(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
