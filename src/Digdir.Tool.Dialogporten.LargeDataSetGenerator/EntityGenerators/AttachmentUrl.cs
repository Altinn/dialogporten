using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CopyCommand;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CsvBuilder;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class AttachmentUrl
{
    private const string Url = "https://digdir.apps.tt02.altinn.no/";

    public static readonly string CopyCommand = Create(nameof(AttachmentUrl),
        "Id", "CreatedAt", "MediaType", "Url", "ConsumerTypeId", "AttachmentId");

    public const string DomainName = nameof(Domain.Dialogporten.Domain.Attachments.AttachmentUrl);

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        foreach (var attachment in Attachment.GetDtos(dto))
        {
            var attachmentUrlId = DeterministicUuidV7.CreateUuidV7(dto.Timestamp, DomainName, attachment.Id.GetHashCode());
            // TODO: Can we build URLs that fetches from the dummy service provider?
            sb.AppendLine($"{attachmentUrlId},{dto.FormattedTimestamp},text/plain,{Url},1,{attachment.Id}");
        }
    });
}
