namespace FitnessCenter.Modules.Booking.Application;

/// <summary>The Booking actions recorded in the audit trail (D-072). The detail is the class (or class type) id.</summary>
internal static class AuditActions
{
    public const string ClassBooked = "class-booked";
    public const string BookingCancelled = "booking-cancelled";
    public const string WaitlistJoined = "waitlist-joined";
    public const string WaitlistLeft = "waitlist-left";
    public const string OfferAccepted = "waitlist-offer-accepted";
    public const string OfferDeclined = "waitlist-offer-declined";

    /// <summary>The system offered a freed spot to the next on the waitlist (D-067). No actor.</summary>
    public const string WaitlistOfferMade = "waitlist-offer-made";

    /// <summary>Staff actions. The subject is the staff member; the detail is the class type or class id.</summary>
    public const string ClassTypeCreated = "class-type-created";
    public const string ClassTypeRenamed = "class-type-renamed";
    public const string ClassScheduled = "class-scheduled";
    public const string ClassChanged = "class-changed";
    public const string ClassCancelled = "class-cancelled";

    /// <summary>A member's booking or waitlist place ended because staff cancelled the class (D-068, D-069).</summary>
    public const string BookingCancelledByStaff = "booking-cancelled-by-staff";
    public const string WaitlistRemovedByStaff = "waitlist-removed-by-staff";

    /// <summary>Staff moved a class the member had booked (D-068).</summary>
    public const string BookingMovedByStaff = "booking-moved-by-staff";
}
