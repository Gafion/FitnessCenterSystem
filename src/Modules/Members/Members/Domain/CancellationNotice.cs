using NodaTime;

namespace FitnessCenter.Modules.Members.Domain;

/// <summary>
/// The notice rule for cancelling a membership (D-048): one month's notice to the end of a
/// month. Dates are Europe/Copenhagen business dates.
/// </summary>
/// <remarks>
/// D-048's example: cancelled on 10 March, the cancellation takes effect at the end of 30 April,
/// and access ends on 1 May. One month from any day of a month falls in the next month
/// (NodaTime clamps 31 January plus one month to 28 or 29 February), so the rule is the last
/// day of the month after the month the cancellation was made in.
/// </remarks>
internal static class CancellationNotice
{
    /// <summary>The last day the member has access; the cancellation takes effect at its end.</summary>
    public static LocalDate EffectiveDate(LocalDate cancelledOn)
    {
        var oneMonthLater = cancelledOn.PlusMonths(1);
        return oneMonthLater.With(DateAdjusters.EndOfMonth);
    }

    /// <summary>
    /// The effective date for a cancellation at <paramref name="cancelledAt"/>, which comes from
    /// the injected clock (rule 2), on that instant's calendar day in <paramref name="businessZone"/>.
    /// </summary>
    public static LocalDate EffectiveDate(Instant cancelledAt, DateTimeZone businessZone) =>
        EffectiveDate(cancelledAt.InZone(businessZone).Date);
}
