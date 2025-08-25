using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

internal static class Attachment
{
    // public static readonly string CopyCommand = CreateCopyCommand(nameof(Attachment),
    //     "Id", "CreatedAt", "UpdatedAt", "Discriminator", "DialogId", "TransmissionId");

    public sealed record AttachmentDto(Guid Id, Guid? DialogId, Guid? TransmissionId);

    public static List<AttachmentDto> GetDtos(DialogTimestamp dto) => BuildDtoList<AttachmentDto>(dtos =>
    {
        // Transmission attachments.
        dtos.AddRange(DialogTransmission.GetDtos(dto)
            .Select(transmission =>
                // Re-use transmission id as attachment id.
                new AttachmentDto(transmission.Id, null, transmission.Id)));

        // Dialog attachments.
        // Re-use dialog id as attachment id.
        dtos.Add(new(dto.DialogId, dto.DialogId, null));
    });

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
