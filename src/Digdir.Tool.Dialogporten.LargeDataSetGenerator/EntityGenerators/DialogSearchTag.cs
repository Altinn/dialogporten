using System.Text;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CopyCommand;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CsvBuilder;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogSearchTag
{
    public static readonly string CopyCommand = Create(nameof(DialogSearchTag),
        "Id", "Value", "CreatedAt", "DialogId");

    private const string DomainName = nameof(Domain.Dialogporten.Domain.Dialogs.Entities.DialogSearchTag);

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        var searchTagId1 = DeterministicUuidV7.Generate(dto.Timestamp, DomainName, 1);
        var searchTagId2 = DeterministicUuidV7.Generate(dto.Timestamp, DomainName, 2);
        var searchTagId3 = DeterministicUuidV7.Generate(dto.Timestamp, DomainName, 3);

        var rng = dto.GetRng();

        var tagText1Index = rng.Next(0, Words.Norwegian.Length);
        var tagText1 = Words.Norwegian.Length != 0
            ? Words.Norwegian[tagText1Index]
            : $"Norsk {Guid.NewGuid().ToString()[..8]}";

        int tagText2Index;
        do
        {
            tagText2Index = rng.Next(0, Words.Norwegian.Length);
        } while (tagText2Index == tagText1Index);

        var tagText2 = Words.Norwegian.Length != 0
            ? Words.Norwegian[tagText2Index]
            : $"Norsk {Guid.NewGuid().ToString()[..8]}";

        var tagText3 = Words.English.Length != 0
            ? Words.English[rng.Next(0, Words.English.Length)]
            : $"English {Guid.NewGuid().ToString()[..8]}";

        sb.AppendLine($"{searchTagId1},{tagText1},{dto.FormattedTimestamp},{dto.DialogId}");
        sb.AppendLine($"{searchTagId2},{tagText2},{dto.FormattedTimestamp},{dto.DialogId}");
        sb.AppendLine($"{searchTagId3},{tagText3},{dto.FormattedTimestamp},{dto.DialogId}");
    });
}
