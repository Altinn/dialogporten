using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;

public sealed record AcceptedLanguage(string LanguageCode, int Weight);

public sealed record AcceptedLanguages(List<AcceptedLanguage>? AcceptedLanguage)
{
    public static bool TryParse(string? value, out AcceptedLanguages acceptedLanguages)
    {
        acceptedLanguages = new AcceptedLanguages([]);

        if (TryParseFromSpan(value, out var temp))
        {
            acceptedLanguages = temp;
            return true;
        }

        return false;
    }

    private static bool TryParseFromSpan(ReadOnlySpan<char> headerSpan, [NotNullWhen(true)] out AcceptedLanguages? acceptedLanguages)
    {
        List<AcceptedLanguage>? internalAcceptedLanguages = [];
        acceptedLanguages = null;
        var range = headerSpan.Split(',');
        while (range.MoveNext())
        {
            var langParts = headerSpan[range.Current];
            var langPartsEnumerator = langParts.Split(";");

            if (!langPartsEnumerator.MoveNext())
            {
                return false;
            }

            var langCode = langParts[langPartsEnumerator.Current];

            var weight = 100;
            if (langPartsEnumerator.MoveNext())
            {
                var weightSpan = langParts[langPartsEnumerator.Current];
                var weightSpanEnumerator = weightSpan.Split("=");

                if (!weightSpanEnumerator.MoveNext() || weightSpan[weightSpanEnumerator.Current] is not "q")
                {
                    return false;
                }

                if (!weightSpanEnumerator.MoveNext())
                {
                    return false;
                }

                if (!float.TryParse(weightSpan[weightSpanEnumerator.Current].Trim(), CultureInfo.InvariantCulture, out var weightFloat)
                 && weightFloat is < 0 or > 1)
                {
                    return false;
                }

                // 0.20000000000003 => 20
                weight = (int)(weightFloat * 100);
            }

            // Normalize to base language code (e.g., en-US/en_US => en)
            var normalized = Localization.NormalizeCultureCode(langCode.Trim().ToString());
            if (string.IsNullOrEmpty(normalized))
            {
                continue;
            }

            internalAcceptedLanguages.Add(new AcceptedLanguage(normalized, weight));
        }

        // Distinct by language code, keep the highest weight per language
        var deduped = internalAcceptedLanguages
            .GroupBy(x => x.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(a => a.Weight).First())
            .ToList();

        acceptedLanguages = new AcceptedLanguages(deduped);
        return true;
    }
}
