using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogActivity(
    Guid Id,
    DateTimeOffset CreatedAt,
    Uri? ExtendedType,
    DialogActivityType.Values TypeId,
    Guid DialogId,
    Guid? TransmissionId
    ) : IEntityGenerator<DialogActivity>
{
    private const DialogActivityType.Values DialogCreated = DialogActivityType.Values.DialogCreated;
    private const DialogActivityType.Values DialogOpened = DialogActivityType.Values.DialogOpened;
    private const DialogActivityType.Values Information = DialogActivityType.Values.Information;

    private const string DomainName = nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Activities.DialogActivity);

    public static IEnumerable<DialogActivity> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            // All dialogs have a DialogCreated activity.
            var dialogCreatedActivityId = timestamp.ToUuidV7(timestamp.DialogId, (int)DialogCreated);
            yield return CreateDialogActivity(dialogCreatedActivityId, timestamp);

            var rng = timestamp.GetRng();

            // Approx. 1/2 of dialogs have a DialogOpened activity.
            if (rng.Next(0, 2) == 0)
            {
                var dialogOpenedActivityId = timestamp.ToUuidV7(timestamp.DialogId, (int)DialogOpened);
                yield return CreateDialogActivity(dialogOpenedActivityId, timestamp);
            }

            // Approx. 1/3 of dialogs have an Information activity.
            if (rng.Next(0, 3) == 0)
            {
                var informationActivityId = timestamp.ToUuidV7(timestamp.DialogId, (int)Information);
                yield return CreateDialogActivity(informationActivityId, timestamp);
            }
        }
    }

    private static DialogActivity CreateDialogActivity(Guid dialogCreatedActivityId, DialogTimestamp timestamp) =>
        new(
            Id: dialogCreatedActivityId,
            CreatedAt: timestamp.Timestamp,
            ExtendedType: null,
            TypeId: DialogCreated,
            DialogId: timestamp.DialogId,
            TransmissionId: null
        );
}
