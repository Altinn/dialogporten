using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CopyCommand;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CsvBuilder;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class LocalizationSet
{
    public static readonly string CopyCommand = Create(nameof(LocalizationSet),
        "Id", "CreatedAt", "Discriminator", "AttachmentId", "GuiActionId", "ActivityId", "DialogContentId", "TransmissionContentId");

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        // Transmission Attachments
        var transmissionId1 = DeterministicUuidV7.Generate(dto.Timestamp, DialogTransmission.DomainName, 1);
        sb.AppendLine($"{transmissionId1},{dto.FormattedTimestamp},AttachmentDisplayName,{transmissionId1},,,,");

        var transmissionId2 = DeterministicUuidV7.Generate(dto.Timestamp, DialogTransmission.DomainName, 2);
        sb.AppendLine($"{transmissionId2},{dto.FormattedTimestamp},AttachmentDisplayName,{transmissionId2},,,,");

        // DialogAttachment
        sb.AppendLine($"{dto.DialogId},{dto.FormattedTimestamp},AttachmentDisplayName,{dto.DialogId},,,,");

        // GuiAction
        var guiActionId1 = DeterministicUuidV7.Generate(dto.Timestamp, DialogGuiAction.DomainName, 1);
        sb.AppendLine($"{guiActionId1},{dto.FormattedTimestamp},DialogGuiActionTitle,,{guiActionId1},,,");

        var guiActionId2 = DeterministicUuidV7.Generate(dto.Timestamp, DialogGuiAction.DomainName, 2);
        sb.AppendLine($"{guiActionId2},{dto.FormattedTimestamp},DialogGuiActionTitle,,{guiActionId2},,,");

        var guiActionId3 = DeterministicUuidV7.Generate(dto.Timestamp, DialogGuiAction.DomainName, 3);
        sb.AppendLine($"{guiActionId3},{dto.FormattedTimestamp},DialogGuiActionTitle,,{guiActionId3},,,");


        // DialogActivity
        // Only information activities have localization entries.
        var informationActivities = Activity
            .GetDtos(dto)
            .Where(x => x.TypeId == DialogActivityType.Values.Information)
            .ToList();

        foreach (var activity in informationActivities)
        {
            sb.AppendLine($"{activity.Id},{dto.FormattedTimestamp},DialogActivityDescription,,,{activity.Id},,");
        }

        // DialogContent
        var dialogContent1 = DeterministicUuidV7.Generate(dto.Timestamp, DialogContent.DomainName, 1);
        sb.AppendLine($"{dialogContent1},{dto.FormattedTimestamp},DialogContentValue,,,,{dialogContent1},");

        var dialogContent2 = DeterministicUuidV7.Generate(dto.Timestamp, DialogContent.DomainName, 2);
        sb.AppendLine($"{dialogContent2},{dto.FormattedTimestamp},DialogContentValue,,,,{dialogContent2},");


        // DialogTransmissionContent
        var transmissionContents = DialogTransmissionContent.GetDtos(dto);
        foreach (var tc in transmissionContents)
        {
            sb.AppendLine($"{tc.Id},{dto.FormattedTimestamp},DialogTransmissionContentValue,,,,,{tc.Id}");
        }
    });
}
