using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Attachment
{
    public const string CopyCommand = """COPY "Attachment" ("Id", "CreatedAt", "UpdatedAt", "Discriminator", "DialogId", "TransmissionId") FROM STDIN (FORMAT csv, HEADER false, NULL '')""";

    public static string Generate(DialogTimestamp dto)
    {
        var attachmentCsvData = new StringBuilder();

        var transmissionId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmission), 1);
        var transmissionId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmission), 2);
        attachmentCsvData.AppendLine($"{transmissionId1},{dto.FormattedTimestamp},{dto.FormattedTimestamp},DialogTransmissionAttachment,,{transmissionId1}");
        attachmentCsvData.AppendLine($"{transmissionId2},{dto.FormattedTimestamp},{dto.FormattedTimestamp},DialogTransmissionAttachment,,{transmissionId2}");

        attachmentCsvData.AppendLine($"{dto.DialogId},{dto.FormattedTimestamp},{dto.FormattedTimestamp},DialogAttachment,{dto.DialogId},");

        return attachmentCsvData.ToString();
    }
}
