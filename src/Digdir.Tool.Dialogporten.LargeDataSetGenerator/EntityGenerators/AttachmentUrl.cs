using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators.CopyCommand;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class AttachmentUrl
{
    public static readonly string CopyCommand = Create(nameof(AttachmentUrl),
        "Id", "CreatedAt", "MediaType", "Url", "ConsumerTypeId", "AttachmentId");

    public static string Generate(DialogTimestamp dto)
    {
        var attachmentUrlCsvData = new StringBuilder();

        var transmissionAttachmentId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission), 1);
        var attachmentUrlId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Attachments.AttachmentUrl), 1);
        var attachmentUrlId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Attachments.AttachmentUrl), 2);
        attachmentUrlCsvData.AppendLine($"{attachmentUrlId1},{dto.FormattedTimestamp},text/plain,https://digdir.apps.tt02.altinn.no/,1,{transmissionAttachmentId1}");
        attachmentUrlCsvData.AppendLine($"{attachmentUrlId2},{dto.FormattedTimestamp},text/plain,https://digdir.apps.tt02.altinn.no/,1,{transmissionAttachmentId1}");

        var transmissionAttachmentId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission), 2);
        var attachmentUrlId3 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Attachments.AttachmentUrl), 3);
        var attachmentUrlId4 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Attachments.AttachmentUrl), 4);
        attachmentUrlCsvData.AppendLine($"{attachmentUrlId3},{dto.FormattedTimestamp},text/plain,https://digdir.apps.tt02.altinn.no/,1,{transmissionAttachmentId2}");
        attachmentUrlCsvData.AppendLine($"{attachmentUrlId4},{dto.FormattedTimestamp},text/plain,https://digdir.apps.tt02.altinn.no/,1,{transmissionAttachmentId2}");

        var attachmentUrlId5 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Attachments.AttachmentUrl), 5);
        var attachmentUrlId6 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Attachments.AttachmentUrl), 6);
        attachmentUrlCsvData.AppendLine($"{attachmentUrlId5},{dto.FormattedTimestamp},text/plain,https://digdir.apps.tt02.altinn.no/,1,{dto.DialogId}");
        attachmentUrlCsvData.AppendLine($"{attachmentUrlId6},{dto.FormattedTimestamp},text/plain,https://digdir.apps.tt02.altinn.no/,1,{dto.DialogId}");

        return attachmentUrlCsvData.ToString();
    }
}
