using NodaTime;

namespace FitnessCenter.Modules.Members.Domain;

/// <summary>Members must be 18 or older (D-052).</summary>
internal static class MinimumAge
{
    public const int Years = 18;

    /// <summary>
    /// The first day on which a person born on <paramref name="dateOfBirth"/> is 18.
    /// Someone born on 29 February turns 18 on 1 March when the 18th year has no 29 February.
    /// UNVERIFIED: no official Danish source for the 29 February case was found; this is the
    /// stricter of the two readings (FIT-67).
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
