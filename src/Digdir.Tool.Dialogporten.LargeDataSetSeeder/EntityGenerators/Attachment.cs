namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record Attachment(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Discriminator,
    DateTimeOffset UpdatedAt,
    Guid? DialogId,
    Guid? TransmissionId
) : IEntityGenerator<Attachment>
{
    public static IEnumerable<Attachment> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            yield return CreateDialogAttachment(timestamp);

            foreach (var transmission in DialogTransmission.GenerateEntities([timestamp]))
            {
                yield return CreateTransmissionAttachment(transmission);
            }
        }
    }

    private static Attachment CreateDialogAttachment(DialogTimestamp timestamp) =>
        new(
            Id: timestamp.DialogId,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            Discriminator: "DialogAttachment",
            DialogId: timestamp.DialogId,
            TransmissionId: null
        );

    private static Attachment CreateTransmissionAttachment(DialogTransmission transmission) =>
        new(
            Id: transmission.Id,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            Discriminator: "DialogTransmissionAttachment",
            DialogId: null,
            TransmissionId: transmission.Id
        );
}
