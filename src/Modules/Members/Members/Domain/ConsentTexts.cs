namespace FitnessCenter.Modules.Members.Domain;

/// <summary>
/// The consent texts shown at signup. Placeholders until the real legal wording exists (U5, D-032).
/// Changing a text means a new version string, so stored consents keep pointing at what was shown.
/// </summary>
internal static class ConsentTexts
{
    public const string TermsVersion = "terms-placeholder-v1";

    public const string Terms =
        "PLACEHOLDER: I accept the membership terms. (Real wording comes before real members sign up.)";

    public const string EarlyStartVersion = "early-start-placeholder-v1";

    public const string EarlyStart =
        "PLACEHOLDER: Start my membership now. I understand that if I withdraw within 14 days, I pay for the days I used.";
}
