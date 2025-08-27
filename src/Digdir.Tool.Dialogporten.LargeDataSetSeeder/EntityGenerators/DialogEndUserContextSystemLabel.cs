using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogEndUserContextSystemLabel(
    Guid DialogEndUserContextId,
    SystemLabel.Values SystemLabelId,
    DateTimeOffset CreatedAt
) : IEntityGenerator<DialogEndUserContextSystemLabel>
{
    public static IEnumerable<DialogEndUserContextSystemLabel> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        return [];
    }
}

