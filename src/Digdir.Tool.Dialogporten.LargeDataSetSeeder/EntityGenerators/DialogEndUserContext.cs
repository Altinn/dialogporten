using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;


internal static class DialogEndUserContext
{
    // public static readonly string CopyCommand = CreateCopyCommand(nameof(DialogEndUserContext),
    //     "Id", "CreatedAt", "UpdatedAt", "Revision", "DialogId");

    public sealed record EndUserContextDto(Guid Id);

    public static EndUserContextDto GetDto(DialogTimestamp dto) =>
        new(dto.ToUuidV7(nameof(DialogEndUserContext)));

    public static string Generate(DialogTimestamp dto) =>
        $"{GetDto(dto).Id}," +
        $"{dto.FormattedTimestamp}," +
        $"{dto.FormattedTimestamp}," +
        $"{Guid.NewGuid()}," +
        $"{dto.DialogId}";
}
