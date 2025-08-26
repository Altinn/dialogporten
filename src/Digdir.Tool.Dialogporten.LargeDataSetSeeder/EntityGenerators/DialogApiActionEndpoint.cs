using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogApiActionEndpoint(

) : IEntityGenerator<DialogApiActionEndpoint>
{
    public static IEnumerable<DialogApiActionEndpoint> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        return [];
    }
}

