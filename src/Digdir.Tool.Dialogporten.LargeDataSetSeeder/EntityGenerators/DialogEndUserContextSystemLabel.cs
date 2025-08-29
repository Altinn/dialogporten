using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogEndUserContextSystemLabel(
    Guid DialogEndUserContextId,
    SystemLabel.Values SystemLabelId,
    DateTimeOffset CreatedAt
) : IEntityGenerator<DialogEndUserContextSystemLabel>
{
    public static IEnumerable<DialogEndUserContextSystemLabel> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            foreach (var context in DialogEndUserContext.GenerateEntities([timestamp]))
            {
                var systemLabelId = Random.Shared.Next(1, 4); // Default/Bin/Archive
                yield return new(
                    DialogEndUserContextId: context.Id,
                    SystemLabelId: (SystemLabel.Values)systemLabelId,
                    CreatedAt: timestamp.Timestamp
                );

                var transmissions = DialogTransmission.GenerateEntities([timestamp]);
                if (transmissions.Any(x => x.TypeId
                        is DialogTransmissionType.Values.Submission
                        or DialogTransmissionType.Values.Correction))
                {
                    yield return new(
                        DialogEndUserContextId: context.Id,
                        SystemLabelId: SystemLabel.Values.Sent,
                        CreatedAt: timestamp.Timestamp
                    );
                }
            }
        }
    }
}

