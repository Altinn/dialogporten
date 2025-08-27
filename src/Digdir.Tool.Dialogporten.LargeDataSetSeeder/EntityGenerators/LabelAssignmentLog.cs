using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record LabelAssignmentLog(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Name,
    string Action,
    Guid ContextId

) : IEntityGenerator<LabelAssignmentLog>
{
    public static IEnumerable<LabelAssignmentLog> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        return [];
    }
}

