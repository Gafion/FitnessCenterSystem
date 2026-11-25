using System.Globalization;

namespace FitnessCenter.Modules.Identity.Application;

/// <summary>
/// The short name other people see for an account: the first name plus the last name's
/// initial, e.g. "Alex E." (D-060). Less personal data than the full name (GDPR data
/// minimisation, CLAUDE.md).
/// </summary>
internal static class DisplayNames
{
    public static string Of(string firstName, string lastName)
    {
        var first = firstName.Trim();
        var last = lastName.Trim();
        if (last.Length == 0)
        {
            return first;
        }

        // A text element, not a char, so a letter outside the BMP or with a combining mark stays whole.
        var initial = StringInfo.GetNextTextElement(last);
        return first.Length == 0 ? $"{initial}." : $"{first} {initial}.";
    }
}
