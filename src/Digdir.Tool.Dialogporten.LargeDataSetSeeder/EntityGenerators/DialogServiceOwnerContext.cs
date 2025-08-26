using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogServiceOwnerContext(

) : IEntityGenerator<DialogServiceOwnerContext>
{
    public static IEnumerable<DialogServiceOwnerContext> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        return [];
    }
}

