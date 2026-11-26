using FitnessCenter.Modules.Booking.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Modules.Booking.Infrastructure;

internal static class ClassLocks
{
    /// <summary>
    /// Locks the class row, then loads the class with its bookings and waitlist. Call it inside
    /// a transaction.
    /// </summary>
    /// <remarks>
    /// The lock makes two requests for the last spot take turns: the second one sees the first
    /// one's booking and gets "full" (D-039). The same lock keeps the waitlist in order, stops a
    /// booking and a queue entry racing (D-051), stops the offer sweep and a request offering
    /// the same spot twice (D-067), and stops a staff change racing a booking (D-068).
    /// </remarks>
    /// <returns>The class, or null when there is no such class.</returns>
    public static async Task<ClassSession?> LockClassAsync(
        this BookingDbContext db, ClassSessionId sessionId, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlAsync(
            $"""SELECT 1 FROM booking.class_sessions WHERE "Id" = {sessionId.Value} FOR UPDATE""",
            cancellationToken);

        return await db.ClassSessions.SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }
}
