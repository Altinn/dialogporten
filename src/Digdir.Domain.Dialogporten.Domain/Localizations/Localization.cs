using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Digdir.Domain.Dialogporten.Domain.Localizations;

public sealed class Localization
{
    private static readonly Dictionary<string, CultureInfo> NeutralCultureByValidCultureCodes =
        BuildNeutralCultureByValidCultureCodes();

    private string _languageCode = null!;

    public string Value { get; set; } = null!;

    public string LanguageCode
    {
        get => _languageCode;
        set => _languageCode = NormalizeCultureCode(value)!;
    }

    // === Dependent relationships ===
    public Guid LocalizationSetId { get; set; }
    public LocalizationSet LocalizationSet { get; set; } = null!;


    [return: NotNullIfNotNull(nameof(cultureCode))]
    public static string? NormalizeCultureCode(string? cultureCode)
    {
        cultureCode = cultureCode?.Trim().Replace('_', '-').ToLowerInvariant();
        return cultureCode is not null && NeutralCultureByValidCultureCodes.TryGetValue(cultureCode, out var neutralCulture)
            ? neutralCulture.TwoLetterISOLanguageName
            : cultureCode;
    }

    public static bool IsValidCultureCode([NotNullWhen(true)] string? cultureCode) =>
        cultureCode is not null
        && NeutralCultureByValidCultureCodes.TryGetValue(cultureCode, out var neutralCulture)
        && cultureCode == neutralCulture.TwoLetterISOLanguageName;

    private static Dictionary<string, CultureInfo> BuildNeutralCultureByValidCultureCodes()
    {
        var exclude = new[] { "no", "iv" };
        var cultureGroups = CultureInfo
            .GetCultures(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures)
            .Where(x => !exclude.Contains(x.TwoLetterISOLanguageName))
            .GroupBy(x => x.TwoLetterISOLanguageName)
            .ToList();

        var neutralCultureByValidCultureCodes = new Dictionary<string, CultureInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var cultureGroup in cultureGroups)
        {
            var neutral = cultureGroup.First(x => x.CultureTypes.HasFlag(CultureTypes.NeutralCultures));
            neutralCultureByValidCultureCodes[neutral.TwoLetterISOLanguageName] = neutral;
            neutralCultureByValidCultureCodes[neutral.ThreeLetterISOLanguageName] = neutral;

            foreach (var culture in cultureGroup.Except([neutral]))
            {
                neutralCultureByValidCultureCodes[culture.Name] = neutral;
            }
        }

        return neutralCultureByValidCultureCodes;
    }
}
