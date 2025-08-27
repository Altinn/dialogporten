using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

#pragma warning disable IDE0072

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record Actor(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    ActorType.Values ActorTypeId,
    string Discriminator,
    Guid? TransmissionId,
    Guid? DialogSeenLogId,
    Guid? ActivityId,
    Guid? LabelAssignmentLogId,
    Guid? ActorNameEntityId
) : IEntityGenerator<Actor>
{
    public static IEnumerable<Actor> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            foreach (var activity in DialogActivity.GenerateEntities([timestamp]))
            {
                yield return CreateDialogActivityActor(activity, timestamp);
            }

            foreach (var seenLog in DialogSeenLog.GenerateEntities([timestamp]))
            {
                yield return CreateDialogSeenLogActor(seenLog, timestamp);
            }

            foreach (var transmission in DialogTransmission.GenerateEntities([timestamp]))
            {
                yield return CreateTransmissionSenderActor(transmission, timestamp);
            }

            foreach (var labelSeenLog in LabelAssignmentLog.GenerateEntities([timestamp]))
            {
                yield return CreateLabelAssignmentLog(labelSeenLog, timestamp);
            }
        }
    }

    private static Actor CreateLabelAssignmentLog(LabelAssignmentLog labelSeenLog, DialogTimestamp timestamp) =>
        CreateActor(
            id: labelSeenLog.Id,
            timestamp: timestamp.Timestamp,
            actorTypeId: ActorType.Values.PartyRepresentative,
            discriminator: "LabelAssignmentLogActor",
            labelAssignmentLogId: labelSeenLog.Id,
            actorNameEntityId: ActorName.GetRandomId());

    private static Actor CreateTransmissionSenderActor(DialogTransmission transmission, DialogTimestamp timestamp)
    {
        Guid? actorNameId = transmission.TypeId switch
        {
            DialogTransmissionType.Values.Submission or
                DialogTransmissionType.Values.Correction => ActorName.GetRandomId(),
            _ => null
        };

        return CreateActor(
            id: transmission.Id,
            timestamp: timestamp.Timestamp,
            actorTypeId: actorNameId.HasValue ? ActorType.Values.ServiceOwner : ActorType.Values.PartyRepresentative,
            discriminator: "DialogTransmissionSenderActor",
            transmissionId: transmission.Id,
            actorNameEntityId: actorNameId);
    }

    private static Actor CreateDialogSeenLogActor(DialogSeenLog seenLog, DialogTimestamp timestamp) =>
        CreateActor(
            id: seenLog.Id,
            timestamp: timestamp.Timestamp,
            actorTypeId: ActorType.Values.PartyRepresentative,
            discriminator: "DialogSeenLogSeenByActor",
            dialogSeenLogId: seenLog.Id,
            actorNameEntityId: ActorName.GetRandomId());

    private static Actor CreateDialogActivityActor(DialogActivity activity, DialogTimestamp timestamp)
    {
        Guid? actorNameId = activity.TypeId switch
        {
            DialogActivityType.Values.DialogCreated or
                DialogActivityType.Values.DialogDeleted or
                DialogActivityType.Values.DialogRestored or
                DialogActivityType.Values.Information => null,
            _ => ActorName.GetRandomId()
        };

        return CreateActor(
            id: activity.Id,
            timestamp: timestamp.Timestamp,
            actorTypeId: actorNameId.HasValue ? ActorType.Values.ServiceOwner : ActorType.Values.PartyRepresentative,
            discriminator: "DialogActivityPerformedByActor",
            activityId: activity.Id,
            actorNameEntityId: actorNameId);
    }

    private static Actor CreateActor(
        Guid id,
        DateTimeOffset timestamp,
        ActorType.Values actorTypeId,
        string discriminator,
        Guid? transmissionId = null,
        Guid? dialogSeenLogId = null,
        Guid? activityId = null,
        Guid? labelAssignmentLogId = null,
        Guid? actorNameEntityId = null
    ) => new(
        Id: id,
        CreatedAt: timestamp,
        UpdatedAt: timestamp,
        ActorTypeId: actorTypeId,
        Discriminator: discriminator,
        TransmissionId: transmissionId,
        DialogSeenLogId: dialogSeenLogId,
        ActivityId: activityId,
        LabelAssignmentLogId: labelAssignmentLogId,
        ActorNameEntityId: actorNameEntityId
    );
}
