using System.Security.Claims;
using System.Text.Json.Serialization;
using FitnessCenter.Modules.Booking.Application;
using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Endpoints;

/// <summary>
/// The staff API for classes and class types (D-068). The Manager policy admits managers and
/// admins (D-062), and needs the authenticator app (D-059).
/// </summary>
internal static class StaffClassEndpoints
{
    /// <summary>The longest list a request may ask for.</summary>
    private static readonly Duration MaxListRange = Duration.FromDays(93);

    /// <summary>The default list: the next 14 days, like the members' schedule (D-039).</summary>
    private static readonly Duration DefaultListRange = BookingPolicy.BookingWindow;

    private const int MaxLengthMinutes = 24 * 60;

    private const int MaxCapacity = 1000;

    public static IEndpointRouteBuilder MapStaffClassEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/booking/staff").WithTags("Booking staff")
            .RequireAuthorization(StaffPolicies.Manager);

        Staff(group.MapGet("/class-types", ListClassTypesAsync).WithName("ListClassTypes"));
        Staff(group.MapPost("/class-types", CreateClassTypeAsync).WithName("CreateClassType"));
        Staff(group.MapPut("/class-types/{classTypeId:guid}", RenameClassTypeAsync).WithName("RenameClassType"));
        Staff(group.MapGet("/classes", ListClassesAsync).WithName("ListStaffClasses"));
        Staff(group.MapGet("/classes/{classId:guid}", GetClassAsync).WithName("GetStaffClass"));
        Staff(group.MapPost("/classes", ScheduleClassAsync).WithName("ScheduleClass"));
        Staff(group.MapPut("/classes/{classId:guid}", ChangeClassAsync).WithName("ChangeClass"));
        Staff(group.MapPost("/classes/{classId:guid}/cancel", CancelClassAsync).WithName("CancelClass"));

        return endpoints;
    }

    private static void Staff(RouteHandlerBuilder endpoint) =>
        endpoint.Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status403Forbidden);

    internal sealed record StaffClassType(Guid Id, string Name);

    /// <param name="Name">1 to 100 characters; leading and trailing spaces are trimmed. Unique, ignoring case.</param>
    internal sealed record ClassTypeRequest(string? Name);

    [JsonConverter(typeof(JsonStringEnumConverter<StaffClassStatus>))]
    internal enum StaffClassStatus
    {
        Scheduled,
        Cancelled,
    }

    /// <param name="InstructorAccountId">An account with the Instructor role (the picker, FIT-105).</param>
    /// <param name="BookedCount">Active bookings.</param>
    /// <param name="WaitlistCount">Accounts waiting or holding an offer (D-051).</param>
    /// <param name="CancelledAt">When the class was cancelled, or null.</param>
    internal sealed record StaffClass(
        Guid ClassId,
        Guid ClassTypeId,
        string ClassName,
        Guid InstructorAccountId,
        string InstructorName,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        int LengthMinutes,
        int Capacity,
        int BookedCount,
        int WaitlistCount,
        StaffClassStatus Status,
        DateTimeOffset? CancelledAt);

    /// <param name="ClassTypeId">One of the class types.</param>
    /// <param name="InstructorAccountId">An account with the Instructor role (the picker, FIT-105).</param>
    /// <param name="StartsAt">An instant in the future, with its offset. A new start time moves the class (D-068).</param>
    /// <param name="LengthMinutes">1 to 1440.</param>
    /// <param name="Capacity">1 to 1000, and never below the booked spots plus the spots held by open offers.</param>
    internal sealed record ClassRequest(
        Guid ClassTypeId, Guid InstructorAccountId, DateTimeOffset StartsAt, int LengthMinutes, int Capacity);

    private static async Task<Ok<StaffClassType[]>> ListClassTypesAsync(
        StaffClassHandler handler, CancellationToken cancellationToken)
    {
        var types = await handler.ListClassTypesAsync(cancellationToken);
        return TypedResults.Ok(types.Select(ToResponse).ToArray());
    }

    private static async Task<Results<Created<StaffClassType>, ValidationProblem, ProblemHttpResult>> CreateClassTypeAsync(
        ClassTypeRequest request, ClaimsPrincipal user, StaffClassHandler handler, CancellationToken cancellationToken)
    {
        if (CheckName(request.Name) is not { } name)
        {
            return NameProblem();
        }

        var (outcome, type) = await handler.CreateClassTypeAsync(name, StaffId(user), cancellationToken);
        return outcome switch
        {
            ClassTypeOutcome.Done => TypedResults.Created($"/api/booking/staff/class-types/{type!.Id.Value}", ToResponse(type)),
            ClassTypeOutcome.NameTaken => NameTaken(),
            _ => throw new InvalidOperationException($"Unknown outcome {outcome}."),
        };
    }

    private static async Task<Results<Ok<StaffClassType>, NotFound, ValidationProblem, ProblemHttpResult>> RenameClassTypeAsync(
        Guid classTypeId, ClassTypeRequest request, ClaimsPrincipal user, StaffClassHandler handler,
        CancellationToken cancellationToken)
    {
        if (CheckName(request.Name) is not { } name)
        {
            return NameProblem();
        }

        var (outcome, type) = await handler.RenameClassTypeAsync(new ClassTypeId(classTypeId), name, StaffId(user), cancellationToken);
        return outcome switch
        {
            ClassTypeOutcome.Done => TypedResults.Ok(ToResponse(type!)),
            ClassTypeOutcome.NotFound => TypedResults.NotFound(),
            ClassTypeOutcome.NameTaken => NameTaken(),
            _ => throw new InvalidOperationException($"Unknown outcome {outcome}."),
        };
    }

    /// <summary>The classes that start from <paramref name="from"/> until before <paramref name="to"/>, soonest first, cancelled ones included.</summary>
    /// <param name="from">Defaults to now.</param>
    /// <param name="to">Defaults to 14 days after <paramref name="from"/>. At most 93 days after it.</param>
    /// <param name="handler">The staff class handler.</param>
    /// <param name="clock">The injected clock.</param>
    /// <param name="cancellationToken">The request's cancellation token.</param>
    private static async Task<Results<Ok<StaffClass[]>, ValidationProblem>> ListClassesAsync(
        DateTimeOffset? from, DateTimeOffset? to, StaffClassHandler handler, IClock clock, CancellationToken cancellationToken)
    {
        var start = from is { } f ? Instant.FromDateTimeOffset(f) : clock.GetCurrentInstant();
        var end = to is { } t ? Instant.FromDateTimeOffset(t) : start + DefaultListRange;
        if (end <= start || end - start > MaxListRange)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["to"] = ["To must be after from, and at most 93 days after it."],
            });
        }

        var classes = await handler.ListClassesAsync(start, end, cancellationToken);
        return TypedResults.Ok(classes.Select(ToResponse).ToArray());
    }

    private static async Task<Results<Ok<StaffClass>, NotFound>> GetClassAsync(
        Guid classId, StaffClassHandler handler, CancellationToken cancellationToken)
    {
        var view = await handler.GetClassAsync(new ClassSessionId(classId), cancellationToken);
        return view is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(view));
    }

    private static async Task<Results<Created<StaffClass>, ValidationProblem, ProblemHttpResult>> ScheduleClassAsync(
        ClassRequest request, ClaimsPrincipal user, StaffClassHandler handler, CancellationToken cancellationToken)
    {
        if (CheckClass(request) is { } invalid)
        {
            return invalid;
        }

        var (outcome, view) = await handler.ScheduleClassAsync(Details(request), StaffId(user), cancellationToken);
        return outcome switch
        {
            StaffClassOutcome.Done => TypedResults.Created($"/api/booking/staff/classes/{view!.Id.Value}", ToResponse(view)),
            StaffClassOutcome.UnknownClassType => UnknownClassType(),
            StaffClassOutcome.NotAnInstructor => NotAnInstructor(),
            StaffClassOutcome.StartsInPast => StartsInPast(),
            _ => throw new InvalidOperationException($"Unknown outcome {outcome}."),
        };
    }

    /// <summary>
    /// Edits the class. A new <c>startsAt</c> moves it: bookings and waitlist stay, and every
    /// booked member is emailed the new time and may cancel until the class starts (D-068).
    /// </summary>
    private static async Task<Results<Ok<StaffClass>, NotFound, ValidationProblem, ProblemHttpResult>> ChangeClassAsync(
        Guid classId, ClassRequest request, ClaimsPrincipal user, StaffClassHandler handler,
        CancellationToken cancellationToken)
    {
        if (CheckClass(request) is { } invalid)
        {
            return invalid;
        }

        var (outcome, view) = await handler.ChangeClassAsync(
            new ClassSessionId(classId), Details(request), StaffId(user), cancellationToken);
        return outcome switch
        {
            StaffClassOutcome.Done => TypedResults.Ok(ToResponse(view!)),
            StaffClassOutcome.NotFound => TypedResults.NotFound(),
            StaffClassOutcome.UnknownClassType => UnknownClassType(),
            StaffClassOutcome.NotAnInstructor => NotAnInstructor(),
            StaffClassOutcome.StartsInPast => StartsInPast(),
            StaffClassOutcome.Started => Conflict("class-started", "This class has already started."),
            StaffClassOutcome.ClassCancelled => Conflict("class-cancelled", "This class is cancelled."),
            StaffClassOutcome.CapacityBelowBookings => Conflict(
                "capacity-below-bookings", "The capacity can't be below the spots already booked or held for the waitlist."),
            _ => throw new InvalidOperationException($"Unknown outcome {outcome}."),
        };
    }

    /// <summary>
    /// Cancels the class: its bookings are cancelled, its waitlist is cleared, and every booked
    /// member is emailed (D-068).
    /// </summary>
    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> CancelClassAsync(
        Guid classId, ClaimsPrincipal user, StaffClassHandler handler, CancellationToken cancellationToken)
    {
        var outcome = await handler.CancelClassAsync(new ClassSessionId(classId), StaffId(user), cancellationToken);
        return outcome switch
        {
            null => TypedResults.NotFound(),
            CancelClassOutcome.Cancelled => TypedResults.NoContent(),
            CancelClassOutcome.Started => Conflict("class-started", "This class has already started."),
            CancelClassOutcome.AlreadyCancelled => Conflict("class-cancelled", "This class is cancelled."),
            _ => throw new InvalidOperationException($"Unknown outcome {outcome}."),
        };
    }

    private static ClassDetails Details(ClassRequest request) => new(
        new ClassTypeId(request.ClassTypeId),
        request.InstructorAccountId,
        Instant.FromDateTimeOffset(request.StartsAt),
        Duration.FromMinutes(request.LengthMinutes),
        request.Capacity);

    private static ValidationProblem? CheckClass(ClassRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.LengthMinutes is < 1 or > MaxLengthMinutes)
        {
            errors["lengthMinutes"] = ["The length must be 1 to 1440 minutes."];
        }

        if (request.Capacity is < 1 or > MaxCapacity)
        {
            errors["capacity"] = ["The capacity must be 1 to 1000."];
        }

        if (request.InstructorAccountId == Guid.Empty)
        {
            errors["instructorAccountId"] = ["Choose an instructor."];
        }

        return errors.Count == 0 ? null : TypedResults.ValidationProblem(errors);
    }

    private static string? CheckName(string? name) => name?.Trim() is { } trimmed && ClassType.IsValidName(trimmed) ? trimmed : null;

    private static ValidationProblem NameProblem() => TypedResults.ValidationProblem(new Dictionary<string, string[]>
    {
        ["name"] = ["The name must be 1 to 100 characters."],
    });

    private static ProblemHttpResult NameTaken() => Conflict("class-type-exists", "A class type with this name already exists.");

    private static ValidationProblem UnknownClassType() => TypedResults.ValidationProblem(new Dictionary<string, string[]>
    {
        ["classTypeId"] = ["There is no such class type."],
    });

    private static ValidationProblem NotAnInstructor() => TypedResults.ValidationProblem(new Dictionary<string, string[]>
    {
        ["instructorAccountId"] = ["The account is not an instructor."],
    });

    private static ValidationProblem StartsInPast() => TypedResults.ValidationProblem(new Dictionary<string, string[]>
    {
        ["startsAt"] = ["The class must start in the future."],
    });

    private static StaffClassType ToResponse(ClassType type) => new(type.Id.Value, type.Name);

    private static StaffClass ToResponse(StaffClassView view) => new(
        view.Id.Value,
        view.ClassTypeId.Value,
        view.ClassName,
        view.InstructorAccountId,
        view.InstructorName,
        view.StartsAt.ToDateTimeOffset(),
        view.EndsAt.ToDateTimeOffset(),
        (int)(view.EndsAt - view.StartsAt).TotalMinutes,
        view.Capacity,
        view.BookedCount,
        view.WaitlistCount,
        view.Status == ClassSessionStatus.Cancelled ? StaffClassStatus.Cancelled : StaffClassStatus.Scheduled,
        view.CancelledAt?.ToDateTimeOffset());

    private static ProblemHttpResult Conflict(string type, string title) =>
        TypedResults.Problem(title: title, statusCode: StatusCodes.Status409Conflict, type: type);

    /// <summary>The logged-in staff member, for the audit trail (D-072). The Manager policy guarantees a login.</summary>
    private static Guid StaffId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("A staff endpoint ran without a login."));
}
