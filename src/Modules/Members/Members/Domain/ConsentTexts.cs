using FitnessCenter.SharedKernel;

namespace FitnessCenter.Modules.Members.Domain;

/// <summary>
/// The consent texts shown at signup, in Danish and English (D-065). Placeholders until the real
/// legal wording exists (U5, D-032, FIT-53). Changing a text means a new version string, so stored
/// consents keep pointing at what was shown. Each language has its own version, so a stored
/// consent also says which language the person read.
/// </summary>
internal static class ConsentTexts
{
    public const string TermsVersion = "terms-placeholder-v1";

    public const string Terms =
        "PLACEHOLDER: I accept the membership terms. (Real wording comes before real members sign up.)";

    public const string EarlyStartVersion = "early-start-placeholder-v1";

    public const string EarlyStart =
        "PLACEHOLDER: Start my membership now. I understand that if I withdraw within 14 days, I pay for the days I used.";

    // UNVERIFIED: the developer's translation of the placeholders, for the owner to check (D-065).
    public const string TermsVersionDanish = "terms-placeholder-v1-da";

    public const string TermsDanish =
        "PLADSHOLDER: Jeg accepterer medlemsbetingelserne. (Den rigtige tekst kommer, før rigtige medlemmer melder sig ind.)";

    public const string EarlyStartVersionDanish = "early-start-placeholder-v1-da";

    public const string EarlyStartDanish =
        "PLADSHOLDER: Start mit medlemskab nu. Jeg forstår, at hvis jeg fortryder inden for 14 dage, betaler jeg for de dage, jeg har brugt.";

    public static (string Version, string Text) TermsIn(Language language) =>
        language == Language.English ? (TermsVersion, Terms) : (TermsVersionDanish, TermsDanish);

    public static (string Version, string Text) EarlyStartIn(Language language) =>
        language == Language.English ? (EarlyStartVersion, EarlyStart) : (EarlyStartVersionDanish, EarlyStartDanish);

    /// <summary>Whether the version is the current terms text in one of the languages.</summary>
    public static bool IsCurrentTermsVersion(string version) => version is TermsVersion or TermsVersionDanish;

    public static bool IsCurrentEarlyStartVersion(string version) => version is EarlyStartVersion or EarlyStartVersionDanish;
}
