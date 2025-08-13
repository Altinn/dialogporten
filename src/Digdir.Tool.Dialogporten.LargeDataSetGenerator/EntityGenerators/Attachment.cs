using System.Text;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators.CopyCommand;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Attachment
{
    public static readonly string CopyCommand = Create(nameof(Attachment),
        "Id", "CreatedAt", "UpdatedAt", "Discriminator", "DialogId", "TransmissionId");

    public static string Generate(DialogTimestamp dto)
    {
        var attachmentCsvData = new StringBuilder();

        var transmissionId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission), 1);
        var transmissionId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission), 2);
        attachmentCsvData.AppendLine($"{transmissionId1},{dto.FormattedTimestamp},{dto.FormattedTimestamp},DialogTransmissionAttachment,,{transmissionId1}");
        attachmentCsvData.AppendLine($"{transmissionId2},{dto.FormattedTimestamp},{dto.FormattedTimestamp},DialogTransmissionAttachment,,{transmissionId2}");

        attachmentCsvData.AppendLine($"{dto.DialogId},{dto.FormattedTimestamp},{dto.FormattedTimestamp},DialogAttachment,{dto.DialogId},");

        return attachmentCsvData.ToString();
    }
}
