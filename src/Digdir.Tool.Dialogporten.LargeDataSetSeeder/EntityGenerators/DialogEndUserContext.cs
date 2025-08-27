using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogEndUserContext(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid DialogId,
    Guid Revision
) : IEntityGenerator<DialogEndUserContext>
{
    public static IEnumerable<DialogEndUserContext> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        return [];
    }
}

// internal static class DialogEndUserContext
// {
//     // public static readonly string CopyCommand = CreateCopyCommand(nameof(DialogEndUserContext),
//     //     "Id", "CreatedAt", "UpdatedAt", "Revision", "DialogId");
//
//     public sealed record EndUserContextDto(Guid Id);
//
//     public static EndUserContextDto GetDto(DialogTimestamp dto) =>
//         new(dto.ToUuidV7(nameof(DialogEndUserContext)));
//
//     public static string Generate(DialogTimestamp dto) =>
//         $"{GetDto(dto).Id}," +
//         $"{dto.FormattedTimestamp}," +
//         $"{dto.FormattedTimestamp}," +
//         $"{Guid.NewGuid()}," +
//         $"{dto.DialogId}";
// }
