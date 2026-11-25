using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using FitnessCenter.Modules.Identity.Contracts;
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
/// <param name="MyOfferExpiresAt">When the spot held for the account runs out (D-067), or null without an open offer.</param>
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
    int? MyWaitlistPlace,
    Instant? MyOfferExpiresAt);

/// <summary>A class the account has booked.</summary>
internal sealed record BookedClass(
    ClassSessionId Id, string ClassName, string InstructorName, Instant StartsAt, Instant EndsAt, Instant CancelUntil);

/// <summary>A spot held for the account from the waitlist, as the account page shows it (D-067).</summary>
internal sealed record OpenOffer(
    ClassSessionId Id, string ClassName, string InstructorName, Instant StartsAt, Instant EndsAt, Instant ExpiresAt);

/// <summary>
/// Booking and cancelling class spots (D-022, D-039), and the waitlist of full classes (D-051). A member who has cancelled can't book
/// classes after their last day of access (D-055); Members answers that through its Contracts (rule 1).
/// Instructors' names come from Identity's Contracts, one batch per list (D-060).
/// </summary>
internal sealed class BookingHandler(
    BookingDbContext db, IMemberDirectory members, IAccountService accounts, IClock clock, OfferEmails offerEmails)
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
        var instructors = await InstructorNamesAsync(sessions, cancellationToken);
        var lastDayOfAccess = accountId is { } account
            ? await members.FindLastDayOfAccessAsync(account, cancellationToken)
            : null;

        return sessions
            .Select(s => new ScheduledClass(
                s.Id,
                names[s.ClassTypeId],
                instructors(s),
                s.StartsAt,
                s.EndsAt,
                s.Capacity,
                s.SpotsLeft,
                accountId is { } id && s.IsBookedBy(id),
                s.CancelUntil,
                s.StartsAfter(lastDayOfAccess),
                accountId is { } waiting ? s.WaitlistPlaceOf(waiting) : null,
                accountId is { } offered ? s.OpenOfferOf(offered, now)?.OfferExpiresAt : null))
            .ToList();
    }

    /// <summary>The spots held for the account that it can still accept, soonest class first (D-067).</summary>
    public async Task<IReadOnlyList<OpenOffer>> GetOpenOffersAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var now = clock.GetCurrentInstant();
        var sessions = await db.ClassSessions
            .AsNoTracking()
            .Where(s => s.StartsAt > now
                && s.Waitlist.Any(e => e.AccountId == accountId && e.Status == WaitlistEntryStatus.Offered && e.OfferExpiresAt > now))
            .OrderBy(s => s.StartsAt)
            .ToListAsync(cancellationToken);
        var names = ClassType.All.ToDictionary(t => t.Id, t => t.Name);
        var instructors = await InstructorNamesAsync(sessions, cancellationToken);

        return sessions
            .Select(s => (Session: s, Offer: s.OpenOfferOf(accountId, now)))
            .Where(pair => pair.Offer is not null)
            .Select(pair => new OpenOffer(
                pair.Session.Id,
                names[pair.Session.ClassTypeId],
                instructors(pair.Session),
                pair.Session.StartsAt,
                pair.Session.EndsAt,
                pair.Offer!.OfferExpiresAt!.Value))
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
        var instructors = await InstructorNamesAsync(sessions, cancellationToken);

        return sessions
            .Select(s => new BookedClass(s.Id, names[s.ClassTypeId], instructors(s), s.StartsAt, s.EndsAt, s.CancelUntil))
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

    /// <returns>The outcome, or null when there is no such class.</returns>
    public async Task<AcceptOfferOutcome?> AcceptOfferAsync(
        ClassSessionId sessionId, Guid accountId, CancellationToken cancellationToken)
    {
        var lastDayOfAccess = await members.FindLastDayOfAccessAsync(accountId, cancellationToken);
        return await ChangeAsync(
            sessionId,
            session => session.AcceptOffer(accountId, clock.GetCurrentInstant(), lastDayOfAccess),
            AcceptOfferOutcome.Booked,
            cancellationToken);
    }

    /// <returns>The outcome, or null when there is no such class.</returns>
    public Task<DeclineOfferOutcome?> DeclineOfferAsync(
        ClassSessionId sessionId, Guid accountId, CancellationToken cancellationToken) =>
        ChangeAsync(
            sessionId,
            session => session.DeclineOffer(accountId, clock.GetCurrentInstant()),
            DeclineOfferOutcome.Declined,
            cancellationToken);

    /// <summary>
    /// Expires the offers that ran out and offers free spots to the next in each queue, in every
    /// class that hasn't started and has a waitlist (D-067). <see cref="WaitlistOfferJob"/> runs it.
    /// </summary>
    /// <returns>How many offers were made.</returns>
    public async Task<int> UpdateOffersAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetCurrentInstant();
        var sessionIds = await db.ClassSessions
            .AsNoTracking()
            .Where(s => s.StartsAt > now
                && s.Waitlist.Any(e => e.Status == WaitlistEntryStatus.Waiting || e.Status == WaitlistEntryStatus.Offered))
            .OrderBy(s => s.StartsAt)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var offered = 0;
        foreach (var sessionId in sessionIds)
        {
            offered += await UpdateOffersAsync(sessionId, cancellationToken);
            db.ChangeTracker.Clear();
        }

        return offered;
    }

    private async Task<int> UpdateOffersAsync(ClassSessionId sessionId, CancellationToken cancellationToken)
    {
        var offers = new List<NewOffer>();
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            var session = await LockAsync(sessionId, cancellationToken);
            if (session is null)
            {
                return 0;
            }

            offers.AddRange(Offers(session, session.UpdateOffers(clock.GetCurrentInstant())));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await offerEmails.SendAsync(offers);
        return offers.Count;
    }

    /// <summary>
    /// Applies one change to a class under its row lock (D-039). Before the change, the class's
    /// offers are brought up to date, so an expired offer can't be accepted and a held spot
    /// can't be booked by someone else. After it, a spot the change freed is offered to the
    /// next person (D-051). Members who got an offer are emailed once it is saved (D-067).
    /// </summary>
    private async Task<TOutcome?> ChangeAsync<TOutcome>(
        ClassSessionId sessionId, Func<ClassSession, TOutcome> change, TOutcome success, CancellationToken cancellationToken)
        where TOutcome : struct, Enum
    {
        var offers = new List<NewOffer>();
        TOutcome outcome;
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            var session = await LockAsync(sessionId, cancellationToken);
            if (session is null)
            {
                return null;
            }

            offers.AddRange(Offers(session, session.UpdateOffers(clock.GetCurrentInstant())));
            outcome = change(session);
            if (EqualityComparer<TOutcome>.Default.Equals(outcome, success))
            {
                offers.AddRange(Offers(session, session.UpdateOffers(clock.GetCurrentInstant())));
            }

            // Saved even when the change is refused: the offers may have moved on.
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await offerEmails.SendAsync(offers);
        return outcome;
    }

    private static IEnumerable<NewOffer> Offers(ClassSession session, IReadOnlyList<WaitlistEntry> entries)
    {
        var name = ClassType.All.Single(t => t.Id == session.ClassTypeId).Name;
        return entries
            .Select(entry => new NewOffer(entry.AccountId, name, session.StartsAt, entry.OfferExpiresAt!.Value))
            .ToList();
    }

    private async Task<ClassSession?> LockAsync(ClassSessionId sessionId, CancellationToken cancellationToken)
    {
        // Lock the class row first, so two requests for the last spot take turns: the second
        // one sees the first one's booking and gets "full" (D-039). The same lock keeps the
        // waitlist in order, stops a booking and a queue entry racing (D-051), and stops the
        // offer sweep and a request offering the same spot twice (D-067).
        await db.Database.ExecuteSqlAsync(
            $"""SELECT 1 FROM booking.class_sessions WHERE "Id" = {sessionId.Value} FOR UPDATE""",
            cancellationToken);

        return await db.ClassSessions.SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }

    /// <summary>
    /// Looks up the instructors' names for the sessions in one call to Identity (D-060). Every
    /// instructor account exists: the foreign key keeps an account with classes from being deleted.
    /// </summary>
    private async Task<Func<ClassSession, string>> InstructorNamesAsync(
        IReadOnlyCollection<ClassSession> sessions, CancellationToken cancellationToken)
    {
        var ids = sessions.Select(s => new AccountId(s.InstructorAccountId)).Distinct().ToList();
        var names = ids.Count == 0
            ? new Dictionary<AccountId, string>()
            : await accounts.FindDisplayNamesAsync(ids, cancellationToken);
        return session => names.GetValueOrDefault(new AccountId(session.InstructorAccountId), string.Empty);
    }
}
