using System.Text;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogContent
{
    public const string CopyCommand = """COPY "DialogContent" ("Id", "CreatedAt", "UpdatedAt", "MediaType", "DialogId", "TypeId") FROM STDIN (FORMAT csv, HEADER false, NULL '')""";

    public static string Generate(DialogTimestamp dto)
    {
        var contentId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Contents.DialogContent), 1);
        var contentId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Contents.DialogContent), 2);

        var dialogContentCsvData = new StringBuilder();

        dialogContentCsvData.AppendLine($"{contentId1},{dto.FormattedTimestamp},{dto.FormattedTimestamp},text/plain,{dto.DialogId},1");
        dialogContentCsvData.AppendLine($"{contentId2},{dto.FormattedTimestamp},{dto.FormattedTimestamp},text/plain,{dto.DialogId},3");

        return dialogContentCsvData.ToString();
    }
}
