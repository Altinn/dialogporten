using System.Diagnostics;
using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators.CopyCommand;


namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Actor
{
    public static readonly string CopyCommand = Create(nameof(Actor),
        "Id", "ActorTypeId", "Discriminator",
        "ActivityId", "DialogSeenLogId", "TransmissionId",
        "CreatedAt", "UpdatedAt", "LabelAssignmentLogId",
        "ActorNameEntityId");

    public static string Generate(DialogTimestamp dto)
    {
        var actorCsvData = new StringBuilder();

        var rng = dto.GetRng();
        var dialogParty = rng.GetParty();
        var transmissionParty = rng.GetParty();

        var dialogPartyActorNameId = ActorName.GetActorNameId(dialogParty);
        var transmissionPartyActorNameId = ActorName.GetActorNameId(transmissionParty);

        // DialogActivity
        // By serviceOwner, no actor name
        var activityId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogActivity), Activity.DialogCreatedType);
        actorCsvData.AppendLine($"{activityId1},2,DialogActivityPerformedByActor,{activityId1},,,{dto.FormattedTimestamp},{dto.FormattedTimestamp},,");

        // By dialog party
        var activityId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogActivity), Activity.InformationType);
        actorCsvData.AppendLine($"{activityId2},1,DialogActivityPerformedByActor,{activityId2},,,{dto.FormattedTimestamp},{dto.FormattedTimestamp},,{dialogPartyActorNameId}");

        // DialogSeenLog
        // By dialog party
        actorCsvData.AppendLine($"{dto.DialogId},1,DialogSeenLogSeenByActor,,{dto.DialogId},,{dto.FormattedTimestamp},{dto.FormattedTimestamp},,{dialogPartyActorNameId}");

        // Transmission
        // By another ActorId/name
        var transmissionId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission), 1);
        actorCsvData.AppendLine($"{transmissionId1},1,DialogTransmissionSenderActor,,,{transmissionId1},{dto.FormattedTimestamp},{dto.FormattedTimestamp},,{transmissionPartyActorNameId}");

        // By service owner, no name
        var transmissionId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission), 2);
        actorCsvData.AppendLine($"{transmissionId2},2,DialogTransmissionSenderActor,,,{transmissionId2},{dto.FormattedTimestamp},{dto.FormattedTimestamp},,");

        return actorCsvData.ToString();
    }
}
