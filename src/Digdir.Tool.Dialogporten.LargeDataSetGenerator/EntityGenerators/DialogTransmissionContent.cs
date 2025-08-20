using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CopyCommand;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CsvBuilder;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogTransmissionContent
{
    public static readonly string CopyCommand = Create(nameof(DialogTransmissionContent),
        "Id", "CreatedAt", "UpdatedAt",
        "MediaType", "TransmissionId", "TypeId");

    private const string DomainName = nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents.DialogTransmissionContent);
    public static List<TransmissionContentDto> GetDtos(DialogTimestamp dto)
    {
        var transmissionDtos = DialogTransmission.GetDtos(dto);
        var contentDtos = new List<TransmissionContentDto>();

        foreach (var transmission in transmissionDtos)
        {
            for (var i = 1; i <= 2; i++)
            {
                var contentId = DeterministicUuidV7.Generate(dto.Timestamp, DomainName, i);
                contentDtos.Add(new TransmissionContentDto(contentId, transmission.Id, i));
            }
        }

        return contentDtos;
    }

    public record TransmissionContentDto(Guid Id, Guid TransmissionId, int TypeId);

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        foreach (var tc in GetDtos(dto))
        {
            sb.AppendLine(
                $"{tc.Id},{dto.FormattedTimestamp},{dto.FormattedTimestamp},text/plain,{tc.TransmissionId},{tc.TypeId}");
        }
    });
}
