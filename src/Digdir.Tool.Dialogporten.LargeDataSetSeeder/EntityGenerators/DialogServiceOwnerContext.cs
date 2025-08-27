namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogServiceOwnerContext(
    Guid DialogId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid Revision
) : IEntityGenerator<DialogServiceOwnerContext>
{
    public static IEnumerable<DialogServiceOwnerContext> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            yield return new(
                DialogId: timestamp.DialogId,
                CreatedAt: timestamp.Timestamp,
                UpdatedAt: timestamp.Timestamp,
                Revision: Guid.NewGuid()
            );
        }
    }
}

