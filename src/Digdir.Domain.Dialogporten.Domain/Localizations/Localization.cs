namespace Digdir.Domain.Dialogporten.Domain.Localizations;

public sealed class Localization
{

    private static readonly HashSet<string> Codes = new(StringComparer.OrdinalIgnoreCase)
    {
        "aa", "ab", "ae", "af", "ak", "am", "an", "ar", "as", "av", "ay", "az",
        "ba", "be", "bg", "bh", "bi", "bm", "bn", "bo", "br", "bs",
        "ca", "ce", "ch", "co", "cr", "cs", "cu", "cv", "cy",
        "da", "de", "dv", "dz",
        "ee", "el", "en", "eo", "es", "et", "eu",
        "fa", "ff", "fi", "fj", "fo", "fr", "fy",
        "ga", "gd", "gl", "gn", "gu", "gv",
        "ha", "he", "hi", "ho", "hr", "ht", "hu", "hy", "hz",
        "ia", "id", "ie", "ig", "ii", "ik", "io", "is", "it", "iu",
        "ja", "jv",
        "ka", "kg", "ki", "kj", "kk", "kl", "km", "kn", "ko", "kr", "ks", "ku", "kv", "kw", "ky",
        "la", "lb", "lg", "li", "ln", "lo", "lt", "lu", "lv",
        "mg", "mh", "mi", "mk", "ml", "mn", "mr", "ms", "mt", "my",
        "na", "nb", "nd", "ne", "ng", "nl", "nn", "nr", "nv", "ny",
        "oc", "oj", "om", "or", "os",
        "pa", "pi", "pl", "ps", "pt",
        "qu",
        "rm", "rn", "ro", "ru", "rw",
        "sa", "sc", "sd", "se", "sg", "si", "sk", "sl", "sm", "sn", "so", "sq", "sr", "ss", "st", "su", "sv", "sw",
        "ta", "te", "tg", "th", "ti", "tk", "tl", "tn", "to", "tr", "ts", "tt", "tw", "ty",
        "ug", "uk", "ur", "uz",
        "ve", "vi", "vo",
        "wa", "wo",
        "xh",
        "yi", "yo",
        "za", "zh", "zu"
    };

    public static bool IsValidCultureCode(string? languageCode) =>
        languageCode is not null && Codes.Contains(languageCode);

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

    public static string? NormalizeCultureCode(string? cultureCode)
    {
        cultureCode = cultureCode?.Trim().Replace('_', '-').ToLowerInvariant().Split('-').FirstOrDefault();
        return cultureCode;
    }

}
