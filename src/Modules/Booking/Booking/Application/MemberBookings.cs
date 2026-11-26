using FitnessCenter.Modules.Booking.Contracts;
using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Application;

/// <summary>
/// Cancels a member's bookings after their membership ends (D-055 amendment, D-057), and takes
/// them off the waitlists of those classes (FIT-80). Members
/// calls it inside its own transaction, so this class opens none: the shared connection
/// (D-015) enlists in the caller's TransactionScope.
/// </summary>
internal sealed class MemberBookings(BookingDbContext db) : IMemberBookings
{
    public async Task<IReadOnlyList<CancelledClassBooking>> CancelBookingsAfterAsync(
        Guid accountId, LocalDate lastDayOfAccess, Instant now, CancellationToken cancellationToken)
    {
        // The first moment after the last day of access, in Copenhagen.
        var from = lastDayOfAccess.PlusDays(1).AtStartOfDayInZone(CenterTime.Zone).ToInstant();
        var sessionIds = await db.ClassSessions
            .AsNoTracking()
            .Where(s => s.StartsAt >= from
                && (s.Bookings.Any(b => b.AccountId == accountId && b.Status == ClassBookingStatus.Booked)
                    || s.Waitlist.Any(e => e.AccountId == accountId
                        && (e.Status == WaitlistEntryStatus.Waiting || e.Status == WaitlistEntryStatus.Offered))))
            .OrderBy(s => s.Id)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        if (sessionIds.Count == 0)
        {
            return [];
        }

        // Lock the class rows as BookingHandler does (D-039), in a fixed order so two
        // requests that lock several rows can't deadlock each other.
        var lockIds = sessionIds.Select(id => id.Value).ToArray();
        await db.Database.ExecuteSqlAsync(
            $"""SELECT 1 FROM booking.class_sessions WHERE "Id" = ANY({lockIds}) ORDER BY "Id" FOR UPDATE""",
            cancellationToken);

        var sessions = await db.ClassSessions
            .Where(s => sessionIds.Contains(s.Id))
            .OrderBy(s => s.StartsAt)
            .ToListAsync(cancellationToken);
        var names = await db.ClassTypes.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);
        foreach (var session in sessions)
        {
            // A queue place for a class the member can no longer attend would get an offer
            // they can't accept, and hold the spot from the next person (FIT-80). The spot an
            // offer held goes to the next person at the next offer update (WaitlistOfferJob).
            session.RemoveFromWaitlistBecauseMembershipEnds(accountId, lastDayOfAccess, now);
        }

        var cancelled = sessions
            .Where(s => s.CancelBecauseMembershipEnds(accountId, lastDayOfAccess, now))
            .Select(s => new CancelledClassBooking(names[s.ClassTypeId], s.StartsAt))
            .ToList();

        await db.SaveChangesAsync(cancellationToken);
        return cancelled;
    }
}
