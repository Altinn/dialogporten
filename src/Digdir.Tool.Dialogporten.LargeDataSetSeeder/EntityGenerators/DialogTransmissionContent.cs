using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogTransmissionContent(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MediaType,
    Guid TransmissionId,
    DialogTransmissionContentType.Values TypeId

) : IEntityGenerator<DialogTransmissionContent>
{
    public static IEnumerable<DialogTransmissionContent> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            foreach (var transmission in DialogTransmission.GenerateEntities([timestamp]))
            {
                yield return CreateTransmissionContent(transmission, timestamp,
                    DialogTransmissionContentType.Values.Title);

                yield return CreateTransmissionContent(transmission, timestamp,
                    DialogTransmissionContentType.Values.Summary);
            }
        }
    }

    private static DialogTransmissionContent CreateTransmissionContent(DialogTransmission transmission,
        DialogTimestamp timestamp, DialogTransmissionContentType.Values typeId) =>
        new(
            Id: timestamp.ToUuidV7<DialogTransmissionContent>(transmission.Id, (int)typeId),
            CreatedAt: timestamp.Timestamp,
            UpdatedAt: timestamp.Timestamp,
            MediaType: "text/plain",
            TransmissionId: transmission.Id,
            TypeId: typeId
        );
}
