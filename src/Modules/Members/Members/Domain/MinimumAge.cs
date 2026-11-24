using NodaTime;

namespace FitnessCenter.Modules.Members.Domain;

/// <summary>Members must be 18 or older (D-052).</summary>
internal static class MinimumAge
{
    public const int Years = 18;

    /// <summary>
    /// The first day on which a person born on <paramref name="dateOfBirth"/> is 18.
    /// Someone born on 29 February turns 18 on 1 March when the 18th year has no 29 February.
    /// UNVERIFIED: a search of Danish official sources (retsinformation.dk, borger.dk) on
    /// 2026-11-24 found no rule on when a person born on 29 February becomes a year older.
    /// The nearest rule found is about limitation periods, not age: a one-year deadline that
    /// starts on 29 February ends on 28 February (forældelsesloven § 27, Den Juridiske
    /// Vejledning A.A.9.4.4). 1 March is the stricter reading and is used until the owner
    /// decides (FIT-67).
    /// </summary>
    public static LocalDate EighteenthBirthday(LocalDate dateOfBirth)
    {
        // NodaTime moves 29 February to 28 February in a non-leap year; move it on to 1 March.
        var birthday = dateOfBirth.PlusYears(Years);
        return birthday.Day == dateOfBirth.Day ? birthday : birthday.PlusDays(1);
    }

    /// <summary>Whether the person is 18 or older on <paramref name="today"/> (a Copenhagen date).</summary>
    public static bool IsMetOn(LocalDate dateOfBirth, LocalDate today) => today >= EighteenthBirthday(dateOfBirth);
}
