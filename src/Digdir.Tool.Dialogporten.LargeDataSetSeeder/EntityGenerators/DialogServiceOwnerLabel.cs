using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogServiceOwnerLabel(
    Guid DialogServiceOwnerContextId,
    DateTimeOffset CreatedAt,
    string Value
) : IEntityGenerator<DialogServiceOwnerLabel>
{
    public static IEnumerable<DialogServiceOwnerLabel> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        return [];
    }
}

