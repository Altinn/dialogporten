using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogEndUserContextSystemLabel(

) : IEntityGenerator<DialogEndUserContextSystemLabel>
{
    public static IEnumerable<DialogEndUserContextSystemLabel> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        return [];
    }
}

