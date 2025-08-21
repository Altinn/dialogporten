using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CopyCommand;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CsvBuilder;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogSearchTag
{
    public static readonly string CopyCommand = Create(nameof(DialogSearchTag),
        "Id", "Value", "CreatedAt", "DialogId");

    public const string DomainName = nameof(Domain.Dialogporten.Domain.Dialogs.Entities.DialogSearchTag);

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        var rng = dto.GetRng();
        var numberOfSearchTags = rng.Next(1, 6);

        var searchTags = Enumerable.Range(0, numberOfSearchTags)
            .Select(i => i % 2 == 0
                ? Words.Norwegian.GetRandomWord(rng)
                : Words.English.GetRandomWord(rng))
            .Distinct()
            .ToList();

        foreach (var tagText in searchTags)
        {
            var searchTagId = DeterministicUuidV7.Generate(dto.Timestamp, DomainName, tagText.GetHashCode());
            sb.AppendLine($"{searchTagId},{tagText},{dto.FormattedTimestamp},{dto.DialogId}");
        }
    });
}
