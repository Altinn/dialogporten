using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator;

internal static class SearchTags
{
    public const string CopyCommand = """COPY "DialogSearchTag" ("Id", "Value", "CreatedAt", "DialogId") FROM STDIN (FORMAT csv, HEADER false, NULL '')""";

    public static string Generate(DialogTimestamp dto)
    {
        var searchTagCsvData = new StringBuilder();

        var searchTagId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogSearchTag), 1);
        var searchTagId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogSearchTag), 2);
        var searchTagId3 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogSearchTag), 3);

        var rng = new Random(dto.DialogId.ToString().GetHashCode());

        var tagText1 = Words.Norwegian.Length != 0
            ? Words.Norwegian[rng.Next(0, Words.Norwegian.Length)]
            : $"Norsk {Guid.NewGuid().ToString()[..8]}";

        var tagText2 = Words.Norwegian.Length != 0
            ? Words.Norwegian[rng.Next(0, Words.Norwegian.Length)]
            : $"Norsk {Guid.NewGuid().ToString()[..8]}";

        var tagText3 = Words.English.Length != 0
            ? Words.English[rng.Next(0, Words.English.Length)]
            : $"English {Guid.NewGuid().ToString()[..8]}";

        searchTagCsvData.AppendLine($"{searchTagId1},{tagText1},{dto.FormattedTimestamp},{dto.DialogId}");
        searchTagCsvData.AppendLine($"{searchTagId2},{tagText2},{dto.FormattedTimestamp},{dto.DialogId}");
        searchTagCsvData.AppendLine($"{searchTagId3},{tagText3},{dto.FormattedTimestamp},{dto.DialogId}");

        return searchTagCsvData.ToString();
    }
}
