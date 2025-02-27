using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class TransmissionContent
{
    public const string CopyCommand = """COPY "DialogTransmissionContent" ("Id", "CreatedAt", "UpdatedAt", "MediaType", "TransmissionId", "TypeId") FROM STDIN (FORMAT csv, HEADER false, NULL '')""";

    public static string Generate(DialogTimestamp dto)
    {
        var csvData = new StringBuilder();

        var transmissionId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmission), 1);
        var contentId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmissionContent), 1);
        var contentId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmissionContent), 2);

        csvData.AppendLine($"{contentId1},{dto.FormattedTimestamp},{dto.FormattedTimestamp},text/plain,{transmissionId1},1");
        csvData.AppendLine($"{contentId2},{dto.FormattedTimestamp},{dto.FormattedTimestamp},text/plain,{transmissionId1},2");

        var transmissionId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmission), 2);
        var contentId3 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmissionContent), 3);
        var contentId4 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmissionContent), 4);

        csvData.AppendLine($"{contentId3},{dto.FormattedTimestamp},{dto.FormattedTimestamp},text/plain,{transmissionId2},1");
        csvData.AppendLine($"{contentId4},{dto.FormattedTimestamp},{dto.FormattedTimestamp},text/plain,{transmissionId2},2");

        return csvData.ToString();
    }
}
