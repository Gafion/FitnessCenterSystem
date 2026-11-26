using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using FitnessCenter.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Application;

/// <summary>What staff give for a class: its type, instructor, time, length and capacity (D-068).</summary>
internal sealed record ClassDetails(
    ClassTypeId TypeId, Guid InstructorAccountId, Instant StartsAt, Duration Length, int Capacity);

/// <summary>A class as the staff screens show it (D-068), cancelled ones included.</summary>
internal sealed record StaffClassView(
    ClassSessionId Id,
    ClassTypeId ClassTypeId,
    string ClassName,
    Guid InstructorAccountId,
    string InstructorName,
    Instant StartsAt,
    Instant EndsAt,
    int Capacity,
    int BookedCount,
    int WaitlistCount,
    ClassSessionStatus Status,
    Instant? CancelledAt);

internal enum StaffClassOutcome
{
    Done,
    NotFound,
    UnknownClassType,

    /// <summary>The instructor account doesn't exist or lacks the Instructor role (D-049, D-060).</summary>
    NotAnInstructor,
    StartsInPast,
    Started,
    ClassCancelled,
    CapacityBelowBookings,
}

internal enum ClassTypeOutcome
{
    Done,
    NotFound,
    NameTaken,
}

/// <summary>
/// Managers and admins create, edit, move and cancel classes, and create and rename class
/// types (D-068), under the Manager policy (D-062). Booked members are emailed after a move or
/// a cancellation; spots a change frees are offered to the waitlist (D-067).
/// </summary>
internal sealed class StaffClassHandler(
    BookingDbContext db,
    BookingHandler bookings,
    IAccountService accounts,
    IClock clock,
    OfferEmails offerEmails,
    ClassChangeEmails changeEmails)
{
    public async Task<IReadOnlyList<ClassType>> ListClassTypesAsync(CancellationToken cancellationToken) =>
        await db.ClassTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken);

    /// <param name="name">Checked by <see cref="ClassType.IsValidName"/>.</param>
    public async Task<(ClassTypeOutcome Outcome, ClassType? Type)> CreateClassTypeAsync(
        string name, CancellationToken cancellationToken)
    {
        if (await NameTakenAsync(name, null, cancellationToken))
        {
            return (ClassTypeOutcome.NameTaken, null);
        }

        var type = ClassType.Create(name);
        db.ClassTypes.Add(type);
        await db.SaveChangesAsync(cancellationToken);
        return (ClassTypeOutcome.Done, type);
    }

    /// <param name="name">Checked by <see cref="ClassType.IsValidName"/>.</param>
    public async Task<(ClassTypeOutcome Outcome, ClassType? Type)> RenameClassTypeAsync(
        ClassTypeId id, string name, CancellationToken cancellationToken)
    {
        var type = await db.ClassTypes.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (type is null)
        {
            return (ClassTypeOutcome.NotFound, null);
        }

        if (await NameTakenAsync(name, id, cancellationToken))
        {
            return (ClassTypeOutcome.NameTaken, null);
        }

        type.Rename(name);
        await db.SaveChangesAsync(cancellationToken);
        return (ClassTypeOutcome.Done, type);
    }

    /// <summary>The classes that start in [<paramref name="from"/>, <paramref name="to"/>), soonest first, cancelled ones included.</summary>
    public async Task<IReadOnlyList<StaffClassView>> ListClassesAsync(
        Instant from, Instant to, CancellationToken cancellationToken)
    {
        var sessions = await db.ClassSessions
            .AsNoTracking()
            .Where(s => s.StartsAt >= from && s.StartsAt < to)
            .OrderBy(s => s.StartsAt)
            .ToListAsync(cancellationToken);
        return await ViewsAsync(sessions, cancellationToken);
    }

    public async Task<StaffClassView?> GetClassAsync(ClassSessionId id, CancellationToken cancellationToken)
    {
        var session = await db.ClassSessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        return session is null ? null : (await ViewsAsync([session], cancellationToken))[0];
    }

    public async Task<(StaffClassOutcome Outcome, StaffClassView? Class)> ScheduleClassAsync(
        ClassDetails details, CancellationToken cancellationToken)
    {
        if (await CheckDetailsAsync(details, cancellationToken) is { } refused)
        {
            return (refused, null);
        }

        if (details.StartsAt <= clock.GetCurrentInstant())
        {
            return (StaffClassOutcome.StartsInPast, null);
        }

        var session = ClassSession.Schedule(
            details.TypeId, details.InstructorAccountId, details.StartsAt, details.Length, details.Capacity);
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return (StaffClassOutcome.Done, await GetClassAsync(session.Id, cancellationToken));
    }

    /// <summary>
    /// Changes the class (D-068). A new start time moves it: bookings and waitlist stay, and
    /// booked members are emailed the new time. More capacity offers the new spots to the waitlist.
    /// </summary>
    public async Task<(StaffClassOutcome Outcome, StaffClassView? Class)> ChangeClassAsync(
        ClassSessionId id, ClassDetails details, CancellationToken cancellationToken)
    {
        if (await CheckDetailsAsync(details, cancellationToken) is { } refused)
        {
            return (await db.ClassSessions.AnyAsync(s => s.Id == id, cancellationToken) ? refused : StaffClassOutcome.NotFound, null);
        }

        var offers = new List<NewOffer>();
        var moved = new List<MovedClassNotice>();
        ClassChange change;
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            var session = await db.LockClassAsync(id, cancellationToken);
            if (session is null)
            {
                return (StaffClassOutcome.NotFound, null);
            }

            var now = clock.GetCurrentInstant();
            offers.AddRange(await bookings.OffersAsync(session, session.UpdateOffers(now), cancellationToken));
            change = session.Change(
                details.TypeId, details.InstructorAccountId, details.StartsAt, details.Length, details.Capacity, now);
            if (change.Outcome == ChangeClassOutcome.Changed)
            {
                offers.AddRange(await bookings.OffersAsync(session, session.UpdateOffers(now), cancellationToken));
                if (change.MovedFrom is { } movedFrom)
                {
                    var name = await ClassNameAsync(session.ClassTypeId, cancellationToken);
                    moved.AddRange(change.BookedAccounts.Select(account =>
                        new MovedClassNotice(account, name, movedFrom, session.StartsAt)));
                }
            }

            // Saved even when the change is refused: the offers may have moved on.
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await offerEmails.SendAsync(offers);
        await changeEmails.SendAsync(moved);
        return change.Outcome switch
        {
            ChangeClassOutcome.Changed => (StaffClassOutcome.Done, await GetClassAsync(id, cancellationToken)),
            ChangeClassOutcome.Started => (StaffClassOutcome.Started, null),
            ChangeClassOutcome.ClassCancelled => (StaffClassOutcome.ClassCancelled, null),
            ChangeClassOutcome.StartsInPast => (StaffClassOutcome.StartsInPast, null),
            ChangeClassOutcome.CapacityBelowBookings => (StaffClassOutcome.CapacityBelowBookings, null),
            _ => throw new InvalidOperationException($"Unknown outcome {change.Outcome}."),
        };
    }

    /// <summary>
    /// Cancels the class (D-068): its bookings are cancelled, its waitlist is cleared, and every
    /// booked and waitlisted member is emailed (D-069).
    /// </summary>
    /// <returns>The outcome, or null when there is no such class.</returns>
    public async Task<CancelClassOutcome?> CancelClassAsync(ClassSessionId id, CancellationToken cancellationToken)
    {
        var notices = new List<CancelledClassNotice>();
        ClassCancellation cancellation;
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            var session = await db.LockClassAsync(id, cancellationToken);
            if (session is null)
            {
                return null;
            }

            cancellation = session.CancelClass(clock.GetCurrentInstant());
            if (cancellation.Outcome == CancelClassOutcome.Cancelled)
            {
                var name = await ClassNameAsync(session.ClassTypeId, cancellationToken);
                notices.AddRange(cancellation.CancelledBookings.Select(account =>
                    new CancelledClassNotice(account, name, session.StartsAt, WasWaitlisted: false)));
                notices.AddRange(cancellation.ClearedWaitlist.Select(account =>
                    new CancelledClassNotice(account, name, session.StartsAt, WasWaitlisted: true)));
                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        await changeEmails.SendAsync(notices);
        return cancellation.Outcome;
    }

    private async Task<StaffClassOutcome?> CheckDetailsAsync(ClassDetails details, CancellationToken cancellationToken)
    {
        if (!await db.ClassTypes.AnyAsync(t => t.Id == details.TypeId, cancellationToken))
        {
            return StaffClassOutcome.UnknownClassType;
        }

        // The instructor must hold the Instructor role itself; Admin and Manager don't imply it (D-062).
        return await accounts.HasRoleAsync(new AccountId(details.InstructorAccountId), StaffRoles.Instructor, cancellationToken)
            ? null
            : StaffClassOutcome.NotAnInstructor;
    }

    private Task<bool> NameTakenAsync(string name, ClassTypeId? except, CancellationToken cancellationToken)
    {
        var lower = name.ToLowerInvariant();
        var sameName = db.ClassTypes.Where(t => t.Name.ToLower() == lower);
        if (except is { } id)
        {
            sameName = sameName.Where(t => t.Id != id);
        }

        return sameName.AnyAsync(cancellationToken);
    }

    private Task<string> ClassNameAsync(ClassTypeId id, CancellationToken cancellationToken) =>
        db.ClassTypes.Where(t => t.Id == id).Select(t => t.Name).SingleAsync(cancellationToken);

    private async Task<IReadOnlyList<StaffClassView>> ViewsAsync(
        IReadOnlyList<ClassSession> sessions, CancellationToken cancellationToken)
    {
        var names = await db.ClassTypes.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);
        var instructors = await bookings.InstructorNamesAsync(sessions, cancellationToken);
        return sessions
            .Select(s => new StaffClassView(
                s.Id,
                s.ClassTypeId,
                names[s.ClassTypeId],
                s.InstructorAccountId,
                instructors(s),
                s.StartsAt,
                s.EndsAt,
                s.Capacity,
                s.BookedCount,
                s.WaitlistCount,
                s.Status,
                s.CancelledAt))
            .ToList();
    }
}
