using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;


public sealed record AttachmentUrl(

) : IEntityGenerator<AttachmentUrl>
{
    public static IEnumerable<AttachmentUrl> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        return [];
    }
}
// internal static class AttachmentUrl
// {
//     private const string Url = "https://digdir.apps.tt02.altinn.no/";
//
//     // public static readonly string CopyCommand = CreateCopyCommand(nameof(AttachmentUrl),
//     //      "Id",
//     //      "CreatedAt",
//     //      "MediaType",
//     //      "Url",
//     //      "ConsumerTypeId",
//     //      "AttachmentId");
//
//     public const string DomainName = nameof(Domain.Dialogporten.Domain.Attachments.AttachmentUrl);
//
//     public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
//     {
//         foreach (var attachment in Attachment.GetDtos(dto))
//         {
//             var attachmentUrlId = dto.ToUuidV7(DomainName, attachment.Id.GetHashCode());
//             // TODO: Can we build URLs that fetches from the dummy service provider?
//             sb.AppendLine(
//                 $"{attachmentUrlId}," +
//                 $"{dto.FormattedTimestamp}," +
//                 $"text/plain," +
//                 $"{Url}," +
//                 $"1," +
//                 $"{attachment.Id}");
//         }
//     });
// }
