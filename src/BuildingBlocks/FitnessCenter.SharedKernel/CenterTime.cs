using NodaTime;

namespace FitnessCenter.SharedKernel;

/// <summary>
/// The center's business rules apply in Danish local time. Instants are stored as UTC;
/// dates such as "member since" are taken in this zone (CLAUDE.md, conventions).
/// </summary>
public static class CenterTime
{
    public static DateTimeZone Zone { get; } = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];
}
