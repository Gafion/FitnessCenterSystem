using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Application;

/// <summary>A class in the schedule, as one account sees it.</summary>
internal sealed record ScheduledClass(
    ClassSessionId Id,
    string ClassName,
    string InstructorName,
    Instant StartsAt,
    Instant EndsAt,
    int Capacity,
    int SpotsLeft,
    bool BookedByMe,
    Instant CancelUntil);

/// <summary>A class the account has booked.</summary>
internal sealed record BookedClass(
    ClassSessionId Id, string ClassName, string InstructorName, Instant StartsAt, Instant EndsAt, Instant CancelUntil);

/// <summary>Booking and cancelling class spots (D-022, D-039).</summary>
internal sealed class BookingHandler(BookingDbContext db, IClock clock)
{
    /// <summary>The classes that start within the booking window, soonest first.</summary>
    public async Task<IReadOnlyList<ScheduledClass>> GetScheduleAsync(Guid? accountId, CancellationToken cancellationToken)
    {
        var now = clock.GetCurrentInstant();
        var until = now + BookingPolicy.BookingWindow;
        var sessions = await db.ClassSessions
            .AsNoTracking()
            .Where(s => s.StartsAt > now && s.StartsAt <= until)
            .OrderBy(s => s.StartsAt)
            .ToListAsync(cancellationToken);
        var names = ClassType.All.ToDictionary(t => t.Id, t => t.Name);

        return sessions
            .Select(s => new ScheduledClass(
                s.Id,
                names[s.ClassTypeId],
                s.InstructorName,
                s.StartsAt,
                s.EndsAt,
                s.Capacity,
                s.SpotsLeft,
                accountId is { } id && s.IsBookedBy(id),
                s.CancelUntil))
            .ToList();
    }

    /// <summary>The account's booked classes that haven't started yet, soonest first (D-022).</summary>
    public async Task<IReadOnlyList<BookedClass>> GetUpcomingBookingsAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var now = clock.GetCurrentInstant();
        var sessions = await db.ClassSessions
            .AsNoTracking()
            .Where(s => s.StartsAt > now
                && s.Bookings.Any(b => b.AccountId == accountId && b.Status == ClassBookingStatus.Booked))
            .OrderBy(s => s.StartsAt)
            .ToListAsync(cancellationToken);
        var names = ClassType.All.ToDictionary(t => t.Id, t => t.Name);

        return sessions
            .Select(s => new BookedClass(s.Id, names[s.ClassTypeId], s.InstructorName, s.StartsAt, s.EndsAt, s.CancelUntil))
            .ToList();
    }

    /// <returns>The outcome, or null when there is no such class.</returns>
    public Task<BookOutcome?> BookAsync(ClassSessionId sessionId, Guid accountId, CancellationToken cancellationToken) =>
        ChangeAsync(sessionId, session => session.Book(accountId, clock.GetCurrentInstant()), BookOutcome.Booked, cancellationToken);

    /// <returns>The outcome, or null when there is no such class.</returns>
    public Task<CancelOutcome?> CancelAsync(ClassSessionId sessionId, Guid accountId, CancellationToken cancellationToken) =>
        ChangeAsync(sessionId, session => session.Cancel(accountId, clock.GetCurrentInstant()), CancelOutcome.Cancelled, cancellationToken);

    private async Task<TOutcome?> ChangeAsync<TOutcome>(
        ClassSessionId sessionId, Func<ClassSession, TOutcome> change, TOutcome success, CancellationToken cancellationToken)
        where TOutcome : struct, Enum
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Lock the class row first, so two requests for the last spot take turns: the second
        // one sees the first one's booking and gets "full" (D-039).
        await db.Database.ExecuteSqlAsync(
            $"""SELECT 1 FROM booking.class_sessions WHERE "Id" = {sessionId.Value} FOR UPDATE""",
            cancellationToken);

        var session = await db.ClassSessions.SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var outcome = change(session);
        if (EqualityComparer<TOutcome>.Default.Equals(outcome, success))
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return outcome;
    }
}
