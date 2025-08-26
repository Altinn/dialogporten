using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogApiAction(

) : IEntityGenerator<DialogApiAction>
{
    public static IEnumerable<DialogApiAction> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        return [];
    }
}

