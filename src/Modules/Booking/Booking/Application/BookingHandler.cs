using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using FitnessCenter.Modules.Members.Contracts;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Application;

/// <summary>A class in the schedule, as one account sees it.</summary>
/// <param name="AfterMyMembershipEnds">
/// The account's member has cancelled and the class starts after the last day of access, so
/// they can't book it (D-055 option A).
/// </param>
/// <param name="MyWaitlistPlace">The account's place in the class's waitlist (D-051), or null.</param>
internal sealed record ScheduledClass(
    ClassSessionId Id,
    string ClassName,
    string InstructorName,
    Instant StartsAt,
    Instant EndsAt,
    int Capacity,
    int SpotsLeft,
    bool BookedByMe,
    Instant CancelUntil,
    bool AfterMyMembershipEnds,
    int? MyWaitlistPlace);

/// <summary>A class the account has booked.</summary>
internal sealed record BookedClass(
    ClassSessionId Id, string ClassName, string InstructorName, Instant StartsAt, Instant EndsAt, Instant CancelUntil);

/// <summary>
/// Booking and cancelling class spots (D-022, D-039), and the waitlist of full classes (D-051). A member who has cancelled can't book
/// classes after their last day of access (D-055); Members answers that through its Contracts (rule 1).
/// </summary>
internal sealed class BookingHandler(BookingDbContext db, IMemberDirectory members, IClock clock)
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
        var lastDayOfAccess = accountId is { } account
            ? await members.FindLastDayOfAccessAsync(account, cancellationToken)
            : null;

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
                s.CancelUntil,
                s.StartsAfter(lastDayOfAccess),
                accountId is { } waiting ? s.WaitlistPlaceOf(waiting) : null))
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
    public async Task<BookOutcome?> BookAsync(ClassSessionId sessionId, Guid accountId, CancellationToken cancellationToken)
    {
        var lastDayOfAccess = await members.FindLastDayOfAccessAsync(accountId, cancellationToken);
        return await ChangeAsync(
            sessionId,
            session => session.Book(accountId, clock.GetCurrentInstant(), lastDayOfAccess),
            BookOutcome.Booked,
            cancellationToken);
    }

    /// <returns>The outcome, or null when there is no such class.</returns>
    public Task<CancelOutcome?> CancelAsync(ClassSessionId sessionId, Guid accountId, CancellationToken cancellationToken) =>
        ChangeAsync(sessionId, session => session.Cancel(accountId, clock.GetCurrentInstant()), CancelOutcome.Cancelled, cancellationToken);

    /// <returns>The outcome, or null when there is no such class.</returns>
    public async Task<JoinWaitlistOutcome?> JoinWaitlistAsync(
        ClassSessionId sessionId, Guid accountId, CancellationToken cancellationToken)
    {
        var lastDayOfAccess = await members.FindLastDayOfAccessAsync(accountId, cancellationToken);
        return await ChangeAsync(
            sessionId,
            session => session.JoinWaitlist(accountId, clock.GetCurrentInstant(), lastDayOfAccess),
            JoinWaitlistOutcome.Joined,
            cancellationToken);
    }

    /// <returns>The outcome, or null when there is no such class.</returns>
    public Task<LeaveWaitlistOutcome?> LeaveWaitlistAsync(
        ClassSessionId sessionId, Guid accountId, CancellationToken cancellationToken) =>
        ChangeAsync(
            sessionId,
            session => session.LeaveWaitlist(accountId, clock.GetCurrentInstant()),
            LeaveWaitlistOutcome.Left,
            cancellationToken);

    private async Task<TOutcome?> ChangeAsync<TOutcome>(
        ClassSessionId sessionId, Func<ClassSession, TOutcome> change, TOutcome success, CancellationToken cancellationToken)
        where TOutcome : struct, Enum
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Lock the class row first, so two requests for the last spot take turns: the second
        // one sees the first one's booking and gets "full" (D-039). The same lock keeps the
        // waitlist in order and stops a booking and a queue entry racing (D-051).
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
