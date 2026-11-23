using System.Security.Claims;
using FitnessCenter.Modules.Booking.Application;
using FitnessCenter.Modules.Booking.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FitnessCenter.Modules.Booking.Endpoints;

internal static class ClassEndpoints
{
    public static IEndpointRouteBuilder MapClassEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/booking").WithTags("Booking");

        group.MapGet("/schedule", GetScheduleAsync).WithName("GetSchedule");
        group.MapPost("/classes/{classId:guid}/booking", BookAsync).WithName("BookClass").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapDelete("/classes/{classId:guid}/booking", CancelAsync).WithName("CancelBooking").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    /// <param name="CancelUntil">After this moment the booking can't be cancelled (D-039).</param>
    internal sealed record ScheduleEntry(
        Guid ClassId,
        string ClassName,
        string InstructorName,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        int Capacity,
        int SpotsLeft,
        bool BookedByMe,
        DateTimeOffset CancelUntil);

    /// <summary>Open to everyone; <c>bookedByMe</c> is only ever true for a logged-in account.</summary>
    private static async Task<Ok<ScheduleEntry[]>> GetScheduleAsync(
        ClaimsPrincipal user, BookingHandler handler, CancellationToken cancellationToken)
    {
        var schedule = await handler.GetScheduleAsync(AccountIdOf(user), cancellationToken);
        return TypedResults.Ok(schedule
            .Select(c => new ScheduleEntry(
                c.Id.Value,
                c.ClassName,
                c.InstructorName,
                c.StartsAt.ToDateTimeOffset(),
                c.EndsAt.ToDateTimeOffset(),
                c.Capacity,
                c.SpotsLeft,
                c.BookedByMe,
                c.CancelUntil.ToDateTimeOffset()))
            .ToArray());
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> BookAsync(
        Guid classId, ClaimsPrincipal user, BookingHandler handler, CancellationToken cancellationToken)
    {
        var outcome = await handler.BookAsync(new ClassSessionId(classId), AccountIdOf(user)!.Value, cancellationToken);
        return outcome switch
        {
            null => TypedResults.NotFound(),
            BookOutcome.Booked => TypedResults.NoContent(),
            BookOutcome.AlreadyBooked => Conflict("already-booked", "You have already booked this class."),
            BookOutcome.Full => Conflict("class-full", "This class is full."),
            BookOutcome.Started => Conflict("class-started", "This class has already started."),
            BookOutcome.NotOpenYet => Conflict("booking-not-open", "Booking opens 14 days before the class."),
            _ => throw new InvalidOperationException($"Unknown outcome {outcome}."),
        };
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> CancelAsync(
        Guid classId, ClaimsPrincipal user, BookingHandler handler, CancellationToken cancellationToken)
    {
        var outcome = await handler.CancelAsync(new ClassSessionId(classId), AccountIdOf(user)!.Value, cancellationToken);
        return outcome switch
        {
            null or CancelOutcome.NotBooked => TypedResults.NotFound(),
            CancelOutcome.Cancelled => TypedResults.NoContent(),
            CancelOutcome.CancellationClosed => Conflict(
                "cancellation-closed", "Cancelling closes 12 hours before the class starts."),
            _ => throw new InvalidOperationException($"Unknown outcome {outcome}."),
        };
    }

    /// <summary>The logged-in account (the Identity cookie's user id), or null.</summary>
    private static Guid? AccountIdOf(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static ProblemHttpResult Conflict(string type, string title) =>
        TypedResults.Problem(title: title, statusCode: StatusCodes.Status409Conflict, type: type);
}
