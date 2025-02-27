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
        var transmissionId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmission), 1);
        localizationSetCsvData.AppendLine($"{transmissionId1},{dto.FormattedTimestamp},AttachmentDisplayName,{transmissionId1},,,,");

        var transmissionId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmission), 2);
        localizationSetCsvData.AppendLine($"{transmissionId2},{dto.FormattedTimestamp},AttachmentDisplayName,{transmissionId2},,,,");


        // DialogAttachment
        localizationSetCsvData.AppendLine($"{dto.DialogId},{dto.FormattedTimestamp},AttachmentDisplayName,{dto.DialogId},,,,");


        // GuiAction
        var guiActionId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogGuiAction), 1);
        localizationSetCsvData.AppendLine($"{guiActionId1},{dto.FormattedTimestamp},DialogGuiActionTitle,,{guiActionId1},,,");

        var guiActionId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogGuiAction), 2);
        localizationSetCsvData.AppendLine($"{guiActionId2},{dto.FormattedTimestamp},DialogGuiActionTitle,,{guiActionId2},,,");

        var guiActionId3 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogGuiAction), 3);
        localizationSetCsvData.AppendLine($"{guiActionId3},{dto.FormattedTimestamp},DialogGuiActionTitle,,{guiActionId3},,,");


        // DialogActivity
        var activityId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogActivity), Activity.DialogCreatedType);
        localizationSetCsvData.AppendLine($"{activityId1},{dto.FormattedTimestamp},DialogActivityDescription,,,{activityId1},,");

        var activityId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogActivity), Activity.InformationType);
        localizationSetCsvData.AppendLine($"{activityId2},{dto.FormattedTimestamp},DialogActivityDescription,,,{activityId2},,");


        // DialogContent
        var dialogContent1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Contents.DialogContent), 1);
        localizationSetCsvData.AppendLine($"{dialogContent1},{dto.FormattedTimestamp},DialogContentValue,,,,{dialogContent1},");

        var dialogContent2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Contents.DialogContent), 2);
        localizationSetCsvData.AppendLine($"{dialogContent2},{dto.FormattedTimestamp},DialogContentValue,,,,{dialogContent2},");


        // DialogTransmissionContent
        var contentId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmissionContent), 1);
        localizationSetCsvData.AppendLine($"{contentId1},{dto.FormattedTimestamp},DialogTransmissionContentValue,,,,,{contentId1}");
        var contentId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmissionContent), 2);
        localizationSetCsvData.AppendLine($"{contentId2},{dto.FormattedTimestamp},DialogTransmissionContentValue,,,,,{contentId2}");
        var contentId3 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmissionContent), 3);
        localizationSetCsvData.AppendLine($"{contentId3},{dto.FormattedTimestamp},DialogTransmissionContentValue,,,,,{contentId3}");
        var contentId4 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmissionContent), 4);
        localizationSetCsvData.AppendLine($"{contentId4},{dto.FormattedTimestamp},DialogTransmissionContentValue,,,,,{contentId4}");

        return localizationSetCsvData.ToString();
    }
}
