using Digdir.Domain.Dialogporten.Domain.Attachments;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record AttachmentUrl(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MediaType,
    string Url,
    AttachmentUrlConsumerType.Values ConsumerTypeId,
    Guid AttachmentId
) : IEntityGenerator<AttachmentUrl>
{
    public static IEnumerable<AttachmentUrl> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            foreach (var attachment in Attachment.GenerateEntities([timestamp]))
            {
                const int numOfAttachmentUrls = 2;

                for (var tieBreaker = 0; tieBreaker < numOfAttachmentUrls; tieBreaker++)
                {
                    yield return CreateAttachmentUrl(timestamp, attachment, tieBreaker);
                }
            }
        }
    }

    private static AttachmentUrl CreateAttachmentUrl(DialogTimestamp timestamp, Attachment attachment, int tieBreaker) =>
        new(
            Id: timestamp.ToUuidV7(attachment.Id, tieBreaker),
            CreatedAt: timestamp.Timestamp,
            UpdatedAt: timestamp.Timestamp,
            MediaType: "text/plain",
            Url: "https://digdir.apps.tt02.altinn.no/",
            ConsumerTypeId: AttachmentUrlConsumerType.Values.Gui,
            AttachmentId: attachment.Id
        );
}
