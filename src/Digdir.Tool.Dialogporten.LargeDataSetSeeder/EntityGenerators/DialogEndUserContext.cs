namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogEndUserContext(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid DialogId,
    Guid Revision
) : IEntityGenerator<DialogEndUserContext>
{
    public static IEnumerable<DialogEndUserContext> GenerateEntities(IEnumerable<DialogTimestamp> timestamps) =>
        timestamps.Select(timestamp =>
            new DialogEndUserContext(
                Id: timestamp.ToUuidV7<DialogEndUserContext>(timestamp.DialogId),
                CreatedAt: timestamp.Timestamp,
                UpdatedAt: timestamp.Timestamp,
                DialogId: timestamp.DialogId,
                Revision: Guid.NewGuid()
            ));
}
