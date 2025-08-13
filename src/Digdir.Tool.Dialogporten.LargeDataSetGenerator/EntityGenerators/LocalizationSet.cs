using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class LocalizationSet
{
    public const string CopyCommand = """COPY "LocalizationSet" ("Id", "CreatedAt", "Discriminator", "AttachmentId", "GuiActionId", "ActivityId", "DialogContentId", "TransmissionContentId") FROM STDIN (FORMAT csv, HEADER false, NULL '')""";

    public static string Generate(DialogTimestamp dto)
    {
        var localizationSetCsvData = new StringBuilder();

        // Transmission Attachments
        var transmissionId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission), 1);
        localizationSetCsvData.AppendLine($"{transmissionId1},{dto.FormattedTimestamp},AttachmentDisplayName,{transmissionId1},,,,");

        var transmissionId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission), 2);
        localizationSetCsvData.AppendLine($"{transmissionId2},{dto.FormattedTimestamp},AttachmentDisplayName,{transmissionId2},,,,");


        // DialogAttachment
        localizationSetCsvData.AppendLine($"{dto.DialogId},{dto.FormattedTimestamp},AttachmentDisplayName,{dto.DialogId},,,,");


        // GuiAction
        var guiActionId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiAction), 1);
        localizationSetCsvData.AppendLine($"{guiActionId1},{dto.FormattedTimestamp},DialogGuiActionTitle,,{guiActionId1},,,");

        var guiActionId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiAction), 2);
        localizationSetCsvData.AppendLine($"{guiActionId2},{dto.FormattedTimestamp},DialogGuiActionTitle,,{guiActionId2},,,");

        var guiActionId3 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiAction), 3);
        localizationSetCsvData.AppendLine($"{guiActionId3},{dto.FormattedTimestamp},DialogGuiActionTitle,,{guiActionId3},,,");


        // DialogActivity
        // Only information activities have localization entries.
        var informationActivities = Activity
            .GetDtos(dto)
            .Where(x => x.TypeId == (int)DialogActivityType.Values.Information)
            .ToList();

        foreach (var activity in informationActivities)
        {
            localizationSetCsvData.AppendLine($"{activity.Id},{dto.FormattedTimestamp},DialogActivityDescription,,,{activity.Id},,");
        }

        // DialogContent
        var dialogContent1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Contents.DialogContent), 1);
        localizationSetCsvData.AppendLine($"{dialogContent1},{dto.FormattedTimestamp},DialogContentValue,,,,{dialogContent1},");

        var dialogContent2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Contents.DialogContent), 2);
        localizationSetCsvData.AppendLine($"{dialogContent2},{dto.FormattedTimestamp},DialogContentValue,,,,{dialogContent2},");


        // DialogTransmissionContent
        var transmissionContents = DialogTransmissionContent.GetDtos(dto);
        foreach (var tc in transmissionContents)
        {
            localizationSetCsvData.AppendLine($"{tc.Id},{dto.FormattedTimestamp},DialogTransmissionContentValue,,,,,{tc.Id}");
        }

        return localizationSetCsvData.ToString();
    }
}
