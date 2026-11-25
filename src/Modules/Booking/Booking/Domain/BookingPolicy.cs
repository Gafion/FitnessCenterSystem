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

    /// <summary>
    /// How long a freed spot is held for the next person on the waitlist (D-067). An offer
    /// never runs past the cancel deadline: it ends at whichever comes first.
    /// </summary>
    public static readonly Duration OfferLength = Duration.FromHours(2);
}
