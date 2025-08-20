using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CopyCommand;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CsvBuilder;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Attachment
{
    public static readonly string CopyCommand = Create(nameof(Attachment),
        "Id", "CreatedAt", "UpdatedAt", "Discriminator", "DialogId", "TransmissionId");

    public record AttachmentDto(Guid Id, Guid? DialogId, Guid? TransmissionId);
    public static List<AttachmentDto> GetDtos(DialogTimestamp dto)
    {
        List<AttachmentDto> dtos = [];

        // Transmission attachments.
        foreach (var transmission in DialogTransmission.GetDtos(dto))
        {
            // Re-use transmission id as attachment id.
            dtos.Add(new(transmission.Id, null, transmission.Id));
        }

        // Dialog attachments.
        // Re-use dialog id as attachment id.
        dtos.Add(new(dto.DialogId, dto.DialogId, null));

        return dtos;
    }

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        foreach (var attachment in GetDtos(dto))
        {
            var discriminator = attachment.TransmissionId.HasValue ? "DialogTransmissionAttachment" : "DialogAttachment";

            var dialogId = attachment.DialogId?.ToString() ?? string.Empty;
            var transmissionId = attachment.TransmissionId?.ToString() ?? string.Empty;

            sb.AppendLine($"{attachment.Id},{dto.FormattedTimestamp},{dto.FormattedTimestamp},{discriminator},{dialogId},{transmissionId}");
        }
    });
}
