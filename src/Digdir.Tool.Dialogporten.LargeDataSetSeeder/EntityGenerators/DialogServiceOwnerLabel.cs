namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogServiceOwnerLabel(
    Guid DialogServiceOwnerContextId,
    DateTimeOffset CreatedAt,
    string Value
) : IEntityGenerator<DialogServiceOwnerLabel>
{
    public static IEnumerable<DialogServiceOwnerLabel> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            foreach (var context in DialogServiceOwnerContext.GenerateEntities([timestamp]))
            {
                var serviceOwnerLabels = Words.GetBetweenZeroAndCountWords(count: 5);
                foreach (var (label, _) in serviceOwnerLabels)
                {
                    yield return new(
                        DialogServiceOwnerContextId: context.DialogId,
                        CreatedAt: timestamp.Timestamp,
                        Value: label
                    );
                }
            }
        }
    }
}

