using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Localization
{
    public static readonly string CopyCommand = CreateCopyCommand(nameof(Localization),
        "LanguageCode", "LocalizationSetId", "CreatedAt", "UpdatedAt", "Value");

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        var rng = dto.GetRng();

        foreach (var localizationSet in LocalizationSet.GetDtos(dto))
        {
            var english = $"{Words.English.GetRandomWord(rng)} {Words.English.GetRandomWord(rng)}";
            var norwegian = $"{Words.Norwegian.GetRandomWord(rng)} {Words.Norwegian.GetRandomWord(rng)}";

            sb.AppendLine($"nb,{localizationSet.Id},{dto.FormattedTimestamp},{dto.FormattedTimestamp},{norwegian}");
            sb.AppendLine($"en,{localizationSet.Id},{dto.FormattedTimestamp},{dto.FormattedTimestamp},{english}");
        }
    });
}
