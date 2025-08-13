using System.Text;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators.CopyCommand;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogTransmissionContent
{
    public static readonly string CopyCommand = Create(nameof(DialogTransmissionContent),
        "Id", "CreatedAt", "UpdatedAt",
        "MediaType", "TransmissionId", "TypeId");

    public static List<TransmissionContentDto> GetDtos(DialogTimestamp dto)
    {
        var transmissionDtos = DialogTransmission.GetDtos(dto);
        var contentDtos = new List<TransmissionContentDto>();

        foreach (var transmission in transmissionDtos)
        {
            for (var i = 1; i <= 2; i++)
            {
                var contentId = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents.DialogTransmissionContent), i);
                contentDtos.Add(new TransmissionContentDto(contentId, transmission.Id, i));
            }
        }

        return contentDtos;
    }

    public record TransmissionContentDto(Guid Id, Guid TransmissionId, int TypeId);

    public static string Generate(DialogTimestamp dto)
    {
        var csvData = new StringBuilder();

        var transmissionContents = GetDtos(dto);
        foreach (var tc in transmissionContents)
        {
            csvData.AppendLine($"{tc.Id},{dto.FormattedTimestamp},{dto.FormattedTimestamp},text/plain,{tc.TransmissionId},{tc.TypeId}");
        }

        return csvData.ToString();
    }
}
