using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Infrastructure;

/// <summary>
/// Synthetic classes for development (D-022: no staff screens in the MVP). A weekly plan is
/// turned into sessions for the booking window, in Copenhagen time. Every class is taught by
/// the synthetic development instructor account, dev-instructor@example.com (D-049, D-060).
/// </summary>
internal static class DevelopmentSchedule
{
    private sealed record WeeklyClass(IsoDayOfWeek Day, LocalTime Time, ClassType Type, int Minutes, int Capacity);

    private static readonly WeeklyClass[] Week =
    [
        new(IsoDayOfWeek.Monday, new LocalTime(7, 0), ClassType.Spinning, 45, 12),
        new(IsoDayOfWeek.Monday, new LocalTime(18, 0), ClassType.CrossFit, 60, 10),
        new(IsoDayOfWeek.Tuesday, new LocalTime(18, 0), ClassType.Spinning, 45, 12),
        new(IsoDayOfWeek.Wednesday, new LocalTime(17, 30), ClassType.CrossFit, 60, 10),
        new(IsoDayOfWeek.Thursday, new LocalTime(7, 0), ClassType.Spinning, 45, 12),
        new(IsoDayOfWeek.Thursday, new LocalTime(18, 0), ClassType.CrossFit, 60, 10),
        new(IsoDayOfWeek.Saturday, new LocalTime(10, 0), ClassType.Spinning, 45, 3),
    ];

    /// <summary>Adds the plan's sessions for the next 14 days that don't exist yet. Safe to run at every startup.</summary>
    /// <param name="instructorAccountId">The development instructor's account, who teaches every class.</param>
    public static async Task EnsureAsync(
        BookingDbContext db, IClock clock, Guid instructorAccountId, CancellationToken cancellationToken)
    {
        var now = clock.GetCurrentInstant();
        var today = now.InZone(CenterTime.Zone).Date;
        var until = now + BookingPolicy.BookingWindow;
        var existing = (await db.ClassSessions
                .Where(s => s.StartsAt > now)
                .Select(s => new { s.ClassTypeId, s.StartsAt })
                .ToListAsync(cancellationToken))
            .ToHashSet();

        for (var date = today; date <= today.PlusDays(14); date = date.PlusDays(1))
        {
            foreach (var plan in Week.Where(p => p.Day == date.DayOfWeek))
            {
                var startsAt = date.At(plan.Time).InZoneLeniently(CenterTime.Zone).ToInstant();
                if (startsAt <= now || startsAt > until || existing.Contains(new { ClassTypeId = plan.Type.Id, StartsAt = startsAt }))
                {
                    continue;
                }

                db.ClassSessions.Add(ClassSession.Schedule(
                    plan.Type, instructorAccountId, startsAt, Duration.FromMinutes(plan.Minutes), plan.Capacity));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
