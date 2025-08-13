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

        // var rng = new Random(dto.DialogId.GetHashCode());
        //
        // var dialogPartyIndex = rng.Next(0, Parties.List.Length);
        // var dialogParty = Parties.List[dialogPartyIndex];

        var dialogParty = dto.GetParty();
        var transmissionPartyIndex = rng.Next(0, Parties.List.Length);
        var transmissionParty = Parties.List[transmissionPartyIndex];

        var dialogPartyActorNameId = ActorName.GetActorNameId(dialogParty);
        if (dialogPartyActorNameId == Guid.Empty) throw new UnreachableException($"ActorNameId for party {dialogParty} should have been seeded");

        var transmissionPartyActorNameId = ActorName.GetActorNameId(transmissionParty);
        if (transmissionPartyActorNameId == Guid.Empty) throw new UnreachableException($"ActorNameId for party {transmissionParty} should have been seeded");

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
