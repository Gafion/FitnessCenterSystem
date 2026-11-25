using NodaTime;

namespace FitnessCenter.Modules.Booking.Domain;

/// <summary>
/// The center's booking rules (D-039). Placeholders until the center answers its own policy
/// (fitness-domain-operations questions 35–36). Full classes have a waitlist (D-051); no penalties.
/// </summary>
internal static class BookingPolicy
{
    /// <summary>How far ahead a class can be booked, and how far ahead the schedule shows.</summary>
    public static readonly Duration BookingWindow = Duration.FromDays(14);

    /// <summary>Cancelling closes this long before the class starts.</summary>
    public static readonly Duration CancellationCutoff = Duration.FromHours(12);
}
