using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogEndUserContext
{
    public static readonly string CopyCommand = CreateCopyCommand(nameof(DialogEndUserContext),
        "Id", "CreatedAt", "UpdatedAt", "Revision", "DialogId", "SystemLabelId");

    public static string Generate(DialogTimestamp dto) =>
        $"{dto.DialogId},{dto.FormattedTimestamp},{dto.FormattedTimestamp},{Guid.NewGuid()},{dto.DialogId},1";
}
