using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

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
        foreach (var timestamp in timestamps)
        {
            var systemLabels = DialogEndUserContextSystemLabel.GenerateEntities([timestamp]).Select((x, i) => (x, i));
            foreach (var (label, tieBreaker) in systemLabels)
            {
                yield return new(
                    Id: timestamp.ToUuidV7<LabelAssignmentLog>(label.DialogEndUserContextId, tieBreaker),
                    CreatedAt: label.CreatedAt,
                    Name: label.SystemLabelId.ToNamespacedName(),
                    Action: "set",
                    ContextId: label.DialogEndUserContextId
                );
            }

        }
    }
}

