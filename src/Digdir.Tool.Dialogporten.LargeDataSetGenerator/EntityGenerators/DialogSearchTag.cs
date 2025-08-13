using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators.CopyCommand;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogSearchTag
{
    public static readonly string CopyCommand = Create(nameof(DialogSearchTag),
        "Id", "Value", "CreatedAt", "DialogId");

    public static string Generate(DialogTimestamp dto)
    {
        var searchTagCsvData = new StringBuilder();

        var searchTagId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.DialogSearchTag), 1);
        var searchTagId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.DialogSearchTag), 2);
        var searchTagId3 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.DialogSearchTag), 3);

        var rng = new Random(dto.DialogId.ToString().GetHashCode());

        var tagText1Index = rng.Next(0, Words.Norwegian.Length);
        var tagText1 = Words.Norwegian.Length != 0
            ? Words.Norwegian[tagText1Index]
            : $"Norsk {Guid.NewGuid().ToString()[..8]}";

        int tagText2Index;
        do
        {
            tagText2Index = rng.Next(0, Words.Norwegian.Length);
        }
        while (tagText2Index == tagText1Index);

        var tagText2 = Words.Norwegian.Length != 0
            ? Words.Norwegian[tagText2Index]
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
