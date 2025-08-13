using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogGuiAction
{
    public const string CopyCommand = """COPY "DialogGuiAction" ("Id", "CreatedAt", "UpdatedAt", "Action", "Url", "AuthorizationAttribute", "IsDeleteDialogAction", "PriorityId", "HttpMethodId", "DialogId") FROM STDIN (FORMAT csv, HEADER false, NULL '')""";

    public static string Generate(DialogTimestamp dto)
    {
        var guiActionCsvData = new StringBuilder();

        var guiActionId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiAction), 1);
        var guiActionId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiAction), 2);
        var guiActionId3 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiAction), 3);

        guiActionCsvData.AppendLine($"{guiActionId1},{dto.FormattedTimestamp},{dto.FormattedTimestamp},submit,https://digdir.apps.tt02.altinn.no,,FALSE,1,2,{dto.DialogId}");
        guiActionCsvData.AppendLine($"{guiActionId2},{dto.FormattedTimestamp},{dto.FormattedTimestamp},submit,https://digdir.apps.tt02.altinn.no,,FALSE,2,2,{dto.DialogId}");
        guiActionCsvData.AppendLine($"{guiActionId3},{dto.FormattedTimestamp},{dto.FormattedTimestamp},submit,https://digdir.apps.tt02.altinn.no,,FALSE,3,2,{dto.DialogId}");

        return guiActionCsvData.ToString();
    }
}
