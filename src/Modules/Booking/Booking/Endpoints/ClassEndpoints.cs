using System.Security.Claims;
using FitnessCenter.Modules.Booking.Application;
using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Identity.Contracts;
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
        group.MapGet("/my-bookings", GetMyBookingsAsync).WithName("GetMyBookings").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapPost("/classes/{classId:guid}/booking", BookAsync).WithName("BookClass").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapDelete("/classes/{classId:guid}/booking", CancelAsync).WithName("CancelBooking").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapPost("/classes/{classId:guid}/waitlist", JoinWaitlistAsync).WithName("JoinWaitlist").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapDelete("/classes/{classId:guid}/waitlist", LeaveWaitlistAsync).WithName("LeaveWaitlist").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapGet("/my-offers", GetMyOffersAsync).WithName("GetMyOffers").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapPost("/classes/{classId:guid}/offer", AcceptOfferAsync).WithName("AcceptOffer").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapDelete("/classes/{classId:guid}/offer", DeclineOfferAsync).WithName("DeclineOffer").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);

        // Instructor policy: the Instructor role only (D-062); each instructor sees their own classes (D-049).
        group.MapGet("/my-classes", GetMyClassesAsync).WithName("GetMyClasses").RequireAuthorization(StaffPolicies.Instructor)
            .Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    /// <param name="BookedNames">The booked members' display names only, in booking order (GDPR minimum).</param>
    internal sealed record MyClass(
        Guid ClassId,
        string ClassName,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        int Capacity,
        string[] BookedNames);

    /// <summary>The logged-in instructor's classes that haven't started yet, soonest first (D-049, D-060).</summary>
    private static async Task<Ok<MyClass[]>> GetMyClassesAsync(
        ClaimsPrincipal user, BookingHandler handler, CancellationToken cancellationToken)
    {
        var classes = await handler.GetTaughtClassesAsync(AccountIdOf(user)!.Value, cancellationToken);
        return TypedResults.Ok(classes
            .Select(c => new MyClass(
                c.Id.Value,
                c.ClassName,
                c.StartsAt.ToDateTimeOffset(),
                c.EndsAt.ToDateTimeOffset(),
                c.Capacity,
                [.. c.BookedNames]))
            .ToArray());
    }

    /// <param name="CancelUntil">
    /// After this moment the booking can't be cancelled (D-039). When the class moved after the
    /// logged-in account booked it, this is the start itself (D-068).
    /// </param>
    /// <param name="AfterMyMembershipEnds">
    /// The logged-in member has cancelled and the class starts after their last day of access,
    /// so it can't be booked (D-055). Always false when logged out.
    /// </param>
    /// <param name="MyWaitlistPlace">
    /// The logged-in account's place in the waitlist, counting from 1 (D-051). Null when it
    /// isn't waiting or is logged out.
    /// </param>
    /// <param name="MyOfferExpiresAt">
    /// A spot in this full class is held for the logged-in account until this moment (D-067);
    /// accepting books it. Null without an open offer or when logged out.
    /// </param>
    internal sealed record ScheduleEntry(
        Guid ClassId,
        string ClassName,
        string InstructorName,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        int Capacity,
        int SpotsLeft,
        bool BookedByMe,
        DateTimeOffset CancelUntil,
        bool AfterMyMembershipEnds,
        int? MyWaitlistPlace,
        DateTimeOffset? MyOfferExpiresAt);

    /// <param name="ExpiresAt">The spot is held until this moment; then it goes to the next person (D-067).</param>
    internal sealed record MyOffer(
        Guid ClassId,
        string ClassName,
        string InstructorName,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        DateTimeOffset ExpiresAt);

    /// <summary>The spots held for the logged-in account from a waitlist, soonest class first (D-067).</summary>
    private static async Task<Ok<MyOffer[]>> GetMyOffersAsync(
        ClaimsPrincipal user, BookingHandler handler, CancellationToken cancellationToken)
    {
        var offers = await handler.GetOpenOffersAsync(AccountIdOf(user)!.Value, cancellationToken);
        return TypedResults.Ok(offers
            .Select(o => new MyOffer(
                o.Id.Value,
                o.ClassName,
                o.InstructorName,
                o.StartsAt.ToDateTimeOffset(),
                o.EndsAt.ToDateTimeOffset(),
                o.ExpiresAt.ToDateTimeOffset()))
            .ToArray());
    }

    /// <summary>Books the spot held for the logged-in account (D-051).</summary>
    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> AcceptOfferAsync(
        Guid classId, ClaimsPrincipal user, BookingHandler handler, CancellationToken cancellationToken)
    {
        var outcome = await handler.AcceptOfferAsync(new ClassSessionId(classId), AccountIdOf(user)!.Value, cancellationToken);
        return outcome switch
        {
            null or AcceptOfferOutcome.NoOffer => TypedResults.NotFound(),
            AcceptOfferOutcome.Booked => TypedResults.NoContent(),
            AcceptOfferOutcome.Expired => Conflict(
                "offer-expired", "The offer has run out, so the spot went to the next person."),
            AcceptOfferOutcome.AfterMembershipEnds => Conflict(
                "after-membership-ends", "Your membership ends before this class, so you can't book it."),
            _ => throw new InvalidOperationException($"Unknown outcome {outcome}."),
        };
    }

    /// <summary>Gives the held spot up, so the next person on the waitlist gets it (D-051).</summary>
    private static async Task<Results<NoContent, NotFound>> DeclineOfferAsync(
        Guid classId, ClaimsPrincipal user, BookingHandler handler, CancellationToken cancellationToken)
    {
        var outcome = await handler.DeclineOfferAsync(new ClassSessionId(classId), AccountIdOf(user)!.Value, cancellationToken);
        return outcome switch
        {
            null or DeclineOfferOutcome.NoOffer => TypedResults.NotFound(),
            DeclineOfferOutcome.Declined => TypedResults.NoContent(),
            _ => throw new InvalidOperationException($"Unknown outcome {outcome}."),
        };
    }

    /// <param name="CancelUntil">
    /// After this moment the booking can't be cancelled (D-039). When the class moved after the
    /// logged-in account booked it, this is the start itself (D-068).
    /// </param>
    internal sealed record MyBooking(
        Guid ClassId,
        string ClassName,
        string InstructorName,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        DateTimeOffset CancelUntil);

    /// <summary>The logged-in account's booked classes that haven't started yet, soonest first.</summary>
    private static async Task<Ok<MyBooking[]>> GetMyBookingsAsync(
        ClaimsPrincipal user, BookingHandler handler, CancellationToken cancellationToken)
    {
        var bookings = await handler.GetUpcomingBookingsAsync(AccountIdOf(user)!.Value, cancellationToken);
        return TypedResults.Ok(bookings
            .Select(b => new MyBooking(
                b.Id.Value,
                b.ClassName,
                b.InstructorName,
                b.StartsAt.ToDateTimeOffset(),
                b.EndsAt.ToDateTimeOffset(),
                b.CancelUntil.ToDateTimeOffset()))
            .ToArray());
    }

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
                c.CancelUntil.ToDateTimeOffset(),
                c.AfterMyMembershipEnds,
                c.MyWaitlistPlace,
                c.MyOfferExpiresAt?.ToDateTimeOffset()))
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
            BookOutcome.AfterMembershipEnds => Conflict(
                "after-membership-ends", "Your membership ends before this class, so you can't book it."),
            BookOutcome.ClassCancelled => Conflict("class-cancelled", "This class is cancelled."),
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

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> JoinWaitlistAsync(
        Guid classId, ClaimsPrincipal user, BookingHandler handler, CancellationToken cancellationToken)
    {
        var outcome = await handler.JoinWaitlistAsync(new ClassSessionId(classId), AccountIdOf(user)!.Value, cancellationToken);
        return outcome switch
        {
            null => TypedResults.NotFound(),
            JoinWaitlistOutcome.Joined => TypedResults.NoContent(),
            JoinWaitlistOutcome.AlreadyWaitlisted => Conflict("already-waitlisted", "You are already on the waitlist for this class."),
            JoinWaitlistOutcome.AlreadyBooked => Conflict("already-booked", "You have already booked this class."),
            JoinWaitlistOutcome.NotFull => Conflict("class-not-full", "This class has free spots, so book it instead."),
            JoinWaitlistOutcome.Started => Conflict("class-started", "This class has already started."),
            JoinWaitlistOutcome.NotOpenYet => Conflict("booking-not-open", "Booking opens 14 days before the class."),
            JoinWaitlistOutcome.AfterMembershipEnds => Conflict(
                "after-membership-ends", "Your membership ends before this class, so you can't book it."),
            JoinWaitlistOutcome.ClassCancelled => Conflict("class-cancelled", "This class is cancelled."),
            _ => throw new InvalidOperationException($"Unknown outcome {outcome}."),
        };
    }

    private static async Task<Results<NoContent, NotFound>> LeaveWaitlistAsync(
        Guid classId, ClaimsPrincipal user, BookingHandler handler, CancellationToken cancellationToken)
    {
        var outcome = await handler.LeaveWaitlistAsync(new ClassSessionId(classId), AccountIdOf(user)!.Value, cancellationToken);
        return outcome switch
        {
            null or LeaveWaitlistOutcome.NotWaitlisted => TypedResults.NotFound(),
            LeaveWaitlistOutcome.Left => TypedResults.NoContent(),
            _ => throw new InvalidOperationException($"Unknown outcome {outcome}."),
        };
    }

    /// <summary>The logged-in account (the Identity cookie's user id), or null.</summary>
    private static Guid? AccountIdOf(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static ProblemHttpResult Conflict(string type, string title) =>
        TypedResults.Problem(title: title, statusCode: StatusCodes.Status409Conflict, type: type);
}
