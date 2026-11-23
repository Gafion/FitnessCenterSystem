namespace FitnessCenter.Modules.Access.Domain;

/// <summary>
/// The id a card reader reads from a card (D-003). The card technology is still open
/// (FIT-33), so this only cleans up what a reader or a person types: no spaces, upper case.
/// </summary>
internal static class CardUid
{
    public const int MaxLength = 64;

    /// <summary>Every simulated card starts with this, so it can never be mistaken for a real one (D-021).</summary>
    public const string SimulatedPrefix = "SIM-";

    /// <returns>The UID as stored, or null when it can't be a UID at all.</returns>
    public static string? Normalize(string? uid)
    {
        var trimmed = uid?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxLength ? null : trimmed;
    }

    /// <summary>A synthetic UID: the prefix and 7 bytes as hex, the length of a common card UID.</summary>
    public static string Simulated(ReadOnlySpan<byte> sevenBytes)
    {
        if (sevenBytes.Length != 7)
        {
            throw new ArgumentException("A simulated UID needs exactly 7 bytes.", nameof(sevenBytes));
        }

        return SimulatedPrefix + Convert.ToHexString(sevenBytes);
    }
}
