using NodaTime;

namespace FitnessCenter.Modules.Members.Domain;

/// <summary>Where a member stands with their payments on one business day (D-020, D-042).</summary>
internal enum PaymentStanding
{
    /// <summary>No payment confirmation yet. Whether this member may check in is not decided (FIT-45).</summary>
    NeverPaid,

    /// <summary>The day is covered by a payment.</summary>
    Paid,

    /// <summary>The paid-until date has passed (D-020). Check-in is refused and one reminder is sent (D-042).</summary>
    Missing,
}

/// <summary>
/// The missing-payment rule (D-020, D-042). D-042 is a placeholder until the center answers
/// D11, so the rule lives here only: a grace period would change this class and nothing else.
/// Dates are Europe/Copenhagen business dates.
/// </summary>
internal static class MissingPayment
{
    /// <summary>No grace period (D-042): the payment is missing from the first day after paid-until.</summary>
    public static LocalDate FirstMissingDay(LocalDate paidUntil) => paidUntil.PlusDays(1);

    /// <summary>
    /// The latest paid-until date whose payment is missing on <paramref name="businessDate"/>.
    /// Every earlier paid-until is missing too. Lets a database query apply the same rule.
    /// </summary>
    public static LocalDate LatestPaidUntilMissingOn(LocalDate businessDate) => businessDate.PlusDays(-1);

    public static PaymentStanding StandingOn(LocalDate? paidUntil, LocalDate businessDate) => paidUntil switch
    {
        null => PaymentStanding.NeverPaid,
        { } date when businessDate >= FirstMissingDay(date) => PaymentStanding.Missing,
        _ => PaymentStanding.Paid,
    };

    /// <summary>
    /// The standing at <paramref name="now"/>, which comes from the injected clock (rule 2),
    /// on that instant's calendar day in <paramref name="businessZone"/>.
    /// </summary>
    public static PaymentStanding StandingAt(LocalDate? paidUntil, Instant now, DateTimeZone businessZone) =>
        StandingOn(paidUntil, now.InZone(businessZone).Date);
}
