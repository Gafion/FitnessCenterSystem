using FitnessCenter.Modules.Booking.Contracts;
using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Application;

/// <summary>
/// Cancels a member's bookings after their membership ends (D-055 amendment, D-057). Members
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
                && s.Bookings.Any(b => b.AccountId == accountId && b.Status == ClassBookingStatus.Booked))
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
        var names = ClassType.All.ToDictionary(t => t.Id, t => t.Name);
        var cancelled = sessions
            .Where(s => s.CancelBecauseMembershipEnds(accountId, lastDayOfAccess, now))
            .Select(s => new CancelledClassBooking(names[s.ClassTypeId], s.StartsAt))
            .ToList();

        await db.SaveChangesAsync(cancellationToken);
        return cancelled;
    }
}
