namespace FitnessCenter.SharedKernel;

/// <summary>The languages the app speaks: Danish by default, English available (D-065).</summary>
public enum Language
{
    Danish,
    English,
}

public static class Languages
{
    public const Language Default = Language.Danish;

    /// <summary>The two-letter code the frontend and the database use: "da" or "en".</summary>
    public static string Code(this Language language) => language == Language.English ? "en" : "da";

    /// <returns>The language for a code ("da", "en", "en-GB"…), or null for any other.</returns>
    public static Language? FromCode(string? code)
    {
        var primary = code?.Split('-')[0].Trim();
        return primary?.ToUpperInvariant() switch
        {
            "DA" => Language.Danish,
            "EN" => Language.English,
            _ => null,
        };
    }

    /// <summary>
    /// The language an <c>Accept-Language</c> header asks for: the supported one with the
    /// highest weight. Danish when the header is missing or names neither (D-065).
    /// </summary>
    public static Language FromAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return Default;
        }

        Language? best = null;
        var bestWeight = 0.0;
        foreach (var part in header.Split(','))
        {
            var pieces = part.Split(';');
            if (FromCode(pieces[0]) is not { } language)
            {
                continue;
            }

            var weight = 1.0;
            foreach (var parameter in pieces.Skip(1))
            {
                var pair = parameter.Split('=', 2);
                if (pair.Length == 2 && pair[0].Trim() == "q"
                    && double.TryParse(pair[1].Trim(), System.Globalization.NumberStyles.AllowDecimalPoint,
                        System.Globalization.CultureInfo.InvariantCulture, out var q))
                {
                    weight = q;
                }
            }

            // Ties keep the earlier entry, as the header lists preferences in order.
            if (weight > bestWeight)
            {
                best = language;
                bestWeight = weight;
            }
        }

        return best ?? Default;
    }
}
