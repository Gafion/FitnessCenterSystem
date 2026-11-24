using NodaTime;

namespace FitnessCenter.Modules.Booking.Contracts;

/// <summary>
/// What other modules may ask the Booking module to do with a member's bookings (rule 1).
/// Members uses it when a member cancels their membership (D-055 amendment, D-057).
/// </summary>
public interface IMemberBookings
{
    /// <summary>
    /// Cancels the account's bookings in classes that start on a Copenhagen day after
    /// <paramref name="lastDayOfAccess"/>, so other members can take the spots (D-055).
    /// Classes on the last day itself keep their bookings. The 12-hour cut-off (D-039) does
    /// not apply: the system cancels, not the member. Runs inside the caller's transaction
    /// when there is one (D-015), so if the caller rolls back, the bookings stay.
    /// </summary>
    /// <param name="now">When the cancellation happens, from the caller's injected clock.</param>
    /// <returns>The cancelled bookings, soonest first. Empty when there were none.</returns>
    Task<IReadOnlyList<CancelledClassBooking>> CancelBookingsAfterAsync(
        Guid accountId, LocalDate lastDayOfAccess, Instant now, CancellationToken cancellationToken);
}

/// <summary>A booking the system cancelled, as the member's confirmation email lists it.</summary>
public sealed record CancelledClassBooking(string ClassName, Instant StartsAt);
