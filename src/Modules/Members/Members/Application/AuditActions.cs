namespace FitnessCenter.Modules.Members.Application;

/// <summary>The Members actions recorded in the audit trail (D-072). The subject is the member's account.</summary>
internal static class AuditActions
{
    /// <summary>The member signed up and the membership started. Detail: <c>password</c> or <c>google</c>.</summary>
    public const string MembershipStarted = "membership-started";

    public const string MembershipCancelled = "membership-cancelled";
    public const string MembershipCancellationUndone = "membership-cancellation-undone";

    /// <summary>Bookings after the last day were cancelled with the membership (FIT-97). Detail: how many.</summary>
    public const string BookingsCancelledWithMembership = "bookings-cancelled-with-membership";

    /// <summary>A payment was recorded for the member. No actor: it comes from the payment service (D-006). No amount.</summary>
    public const string PaymentRecorded = "payment-recorded";
}
