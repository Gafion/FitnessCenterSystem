using CsCheck;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Tests;

/// <summary>
/// Property tests (D-036) for the notice rule (D-048). The generators aim at month ends, where
/// month lengths differ, and at instants near Copenhagen midnight.
/// </summary>
[TestClass]
public sealed class CancellationNoticeProperties
{
    private static readonly LocalDate Epoch = new(2024, 1, 1);

    private static readonly Gen<LocalDate> AnyDay = Gen.Int[0, 3650].Select(days => Epoch.PlusDays(days));

    /// <summary>Any day, or a day within two of a month end.</summary>
    private static readonly Gen<LocalDate> CancelledOn = Gen.OneOf(
        AnyDay,
        Gen.Select(AnyDay, Gen.Int[-2, 2]).Select((day, shift) => day.With(DateAdjusters.EndOfMonth).PlusDays(shift)));

    /// <summary>Instants near a Copenhagen midnight: ±1 second, ±10 minutes, ±3 hours, or anywhere that day.</summary>
    private static readonly Gen<Instant> CancelledAt = Gen.Select(
            CancelledOn,
            Gen.OneOf(Gen.Long[-1, 1], Gen.Long[-600, 600], Gen.Long[-3 * 3600, 3 * 3600], Gen.Long[-86_400, 86_400]))
        .Select((day, seconds) => CenterTime.Zone.AtStartOfDay(day).ToInstant().Plus(Duration.FromSeconds(seconds)));

    [TestMethod]
    public void The_effective_date_is_always_the_last_day_of_a_month()
    {
        CancelledOn.Sample(day =>
        {
            var effective = CancellationNotice.EffectiveDate(day);
            return effective.PlusDays(1).Day == 1;
        });
    }

    [TestMethod]
    public void The_effective_date_is_always_at_least_one_month_after_the_cancellation()
    {
        CancelledOn.Sample(day => CancellationNotice.EffectiveDate(day) >= day.PlusMonths(1));
    }

    /// <summary>Notice never runs past the end of the following month (D-048 "to the end of a month").</summary>
    [TestMethod]
    public void The_effective_date_is_the_end_of_the_month_after_the_cancellation()
    {
        CancelledOn.Sample(day =>
        {
            var nextMonth = new LocalDate(day.Year, day.Month, 1).PlusMonths(1);
            var effective = CancellationNotice.EffectiveDate(day);
            return effective.Year == nextMonth.Year && effective.Month == nextMonth.Month;
        });
    }

    /// <summary>An instant's effective date is the one for its Copenhagen calendar day.</summary>
    [TestMethod]
    public void An_instant_uses_its_Copenhagen_date()
    {
        CancelledAt.Sample(instant =>
            CancellationNotice.EffectiveDate(instant, CenterTime.Zone)
                == CancellationNotice.EffectiveDate(instant.InZone(CenterTime.Zone).Date));
    }

    /// <summary>The effective instant (the end of the effective date in Copenhagen) is at least a month away.</summary>
    [TestMethod]
    public void Access_ends_at_least_one_month_after_the_cancellation_instant()
    {
        CancelledAt.Sample(instant =>
        {
            var accessEnds = CenterTime.Zone
                .AtStartOfDay(CancellationNotice.EffectiveDate(instant, CenterTime.Zone).PlusDays(1)).ToInstant();
            return accessEnds.InZone(CenterTime.Zone).LocalDateTime
                >= instant.InZone(CenterTime.Zone).LocalDateTime.PlusMonths(1);
        });
    }
}
