namespace Altinn.ApiClients.Dialogporten.Common;

internal static class AcceptedLanguagesHeaderFormatter
{
    public static string FormatAcceptedLanguages<T>(
        ICollection<T>? acceptedLanguages,
        Func<T, string> formatAcceptedLanguage) =>
        acceptedLanguages is null || acceptedLanguages.Count == 0
            ? string.Empty
            : string.Join(", ", acceptedLanguages.Select(formatAcceptedLanguage));

    public static string FormatAcceptedLanguage(string languageCode, int weight) =>
        weight >= 100
            ? languageCode
            : $"{languageCode};q={weight / 100.0:0.##}";
}
