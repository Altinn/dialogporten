using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogTransmissionContent
{
    public static readonly string CopyCommand = CreateCopyCommand(nameof(DialogTransmissionContent),
        "Id", "CreatedAt", "UpdatedAt",
        "MediaType", "TransmissionId", "TypeId");

    public const string DomainName = nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents.DialogTransmissionContent);

    public static List<TransmissionContentDto> GetDtos(DialogTimestamp dto) => BuildDtoList<TransmissionContentDto>(dtos =>
    {
        foreach (var transmission in DialogTransmission.GetDtos(dto))
        {
            for (var i = 1; i <= 2; i++)
            {
                var contentId = dto.UuidV7(DomainName, i);
                dtos.Add(new TransmissionContentDto(contentId, transmission.Id, i));
            }
        }
    });

    public sealed record TransmissionContentDto(Guid Id, Guid TransmissionId, int TypeId);

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        foreach (var tc in GetDtos(dto))
        {
            sb.AppendLine($"{tc.Id},{dto.FormattedTimestamp},{dto.FormattedTimestamp},text/plain,{tc.TransmissionId},{tc.TypeId}");
        }
    });
}
