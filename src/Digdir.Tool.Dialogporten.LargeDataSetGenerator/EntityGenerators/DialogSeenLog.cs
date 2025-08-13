using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators.CopyCommand;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogSeenLog
{
    public static readonly string CopyCommand = Create(nameof(DialogSeenLog),
        "Id", "CreatedAt", "IsViaServiceOwner", "DialogId", "EndUserTypeId");

    public static string Generate(DialogTimestamp dto)
        => $"{dto.DialogId},{dto.FormattedTimestamp},FALSE,{dto.DialogId},1";
}
