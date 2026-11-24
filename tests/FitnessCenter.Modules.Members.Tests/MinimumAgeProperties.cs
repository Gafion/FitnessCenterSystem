using CsCheck;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Tests;

/// <summary>
/// Property tests (D-036) for the 18+ rule (D-052). The generators aim at the 18th birthday,
/// at 29 February and at Copenhagen midnight, where a UTC date would be a day off.
/// </summary>
[TestClass]
public sealed class MinimumAgeProperties
{
    private static readonly LocalDate Epoch = new(1950, 1, 1);

    private static readonly Gen<LocalDate> AnyBirth = Gen.Int[0, 365 * 80].Select(days => Epoch.PlusDays(days));

    /// <summary>29 February of a leap year between 1952 and 2028.</summary>
    private static readonly Gen<LocalDate> LeapDayBirth = Gen.Int[0, 19].Select(i => new LocalDate(1952 + (4 * i), 2, 29));

    private static readonly Gen<LocalDate> Birth = Gen.OneOf(AnyBirth, LeapDayBirth);

    /// <summary>A day offset from the 18th birthday: mostly next to it, sometimes far away.</summary>
    private static readonly Gen<int> DayOffset = Gen.OneOf(Gen.Int[-2, 2], Gen.Int[-3000, 3000]);

    [TestMethod]
    public void Eighteen_is_met_exactly_from_the_18th_birthday()
    {
        Gen.Select(Birth, DayOffset).Sample((born, offset) =>
        {
            var birthday = MinimumAge.EighteenthBirthday(born);
            return MinimumAge.IsMetOn(born, birthday.PlusDays(offset)) == (offset >= 0);
        });
    }

    [TestMethod]
    public void The_18th_birthday_is_the_same_day_and_month_18_years_on_or_1_March()
    {
        Birth.Sample(born =>
        {
            var birthday = MinimumAge.EighteenthBirthday(born);
            var sameDay = birthday.Year == born.Year + 18 && birthday.Month == born.Month && birthday.Day == born.Day;
            var leapDayMovedOn = born is { Month: 2, Day: 29 } && birthday == new LocalDate(born.Year + 18, 3, 1);
            return sameDay || leapDayMovedOn;
        });
    }

    [TestMethod]
    public void Someone_born_on_29_February_is_refused_on_28_February_and_accepted_on_1_March()
    {
        LeapDayBirth.Sample(born =>
            !MinimumAge.IsMetOn(born, new LocalDate(born.Year + 18, 2, 28))
            && MinimumAge.IsMetOn(born, new LocalDate(born.Year + 18, 3, 1)));
    }

    /// <summary>
    /// Signup at any instant within two days of Copenhagen midnight on the 18th birthday is
    /// accepted exactly when the instant is on or after that midnight.
    /// </summary>
    [TestMethod]
    public void Signup_is_accepted_from_Copenhagen_midnight_on_the_18th_birthday()
    {
        var seconds = Gen.OneOf(Gen.Long[-1, 1], Gen.Long[-3 * 3600, 3 * 3600], Gen.Long[-2 * 86_400, 2 * 86_400]);

        Gen.Select(Birth, seconds).Sample((born, fromMidnight) =>
        {
            var midnight = MinimumAge.EighteenthBirthday(born).AtStartOfDayInZone(CenterTime.Zone).ToInstant();
            var now = midnight.Plus(Duration.FromSeconds(fromMidnight));
            var consents = new SignupConsents(TermsAccepted: true, "terms-v-test", EarlyStartRequested: false, "early-v-test");

            bool accepted;
            try
            {
                Member.SignUp(Guid.NewGuid(), MembershipType.General, consents, now, CenterTime.Zone, born);
                accepted = true;
            }
            catch (InvalidOperationException)
            {
                accepted = false;
            }

            return accepted == (fromMidnight >= 0);
        });
    }
}
