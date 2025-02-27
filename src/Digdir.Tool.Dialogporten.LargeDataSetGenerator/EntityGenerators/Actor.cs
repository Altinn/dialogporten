using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Actor
{
    public const string CopyCommand = """COPY "Actor" ("Id", "ActorId", "ActorTypeId", "ActorName", "Discriminator", "ActivityId", "DialogSeenLogId", "TransmissionId", "CreatedAt", "UpdatedAt", "LabelAssignmentLogId") FROM STDIN (FORMAT csv, HEADER false, NULL '')""";

    public static string Generate(DialogTimestamp dto)
    {
        var actorCsvData = new StringBuilder();

        // DialogActivity
        var activityId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogActivity), Activity.DialogCreatedType);
        actorCsvData.AppendLine($"{activityId1},,1,ActorName,DialogActivityPerformedByActor,{activityId1},,,{dto.FormattedTimestamp},{dto.FormattedTimestamp},");
        var activityId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogActivity), Activity.InformationType);
        actorCsvData.AppendLine($"{activityId2},,1,ActorName,DialogActivityPerformedByActor,{activityId2},,,{dto.FormattedTimestamp},{dto.FormattedTimestamp},");

        // DialogSeenLog
        var rng = new Random(dto.DialogId.GetHashCode());
        var partyIndex = rng.Next(0, Parties.List.Length);
        var party = Parties.List[partyIndex];

        actorCsvData.AppendLine($"{dto.DialogId},{party},1,ActorName,DialogSeenLogSeenByActor,,{dto.DialogId},,{dto.FormattedTimestamp},{dto.FormattedTimestamp},");

        // Transmission
        var transmissionId1 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmission), 1);
        actorCsvData.AppendLine($"{transmissionId1},,1,ActorName,DialogTransmissionSenderActor,,,{transmissionId1},{dto.FormattedTimestamp},{dto.FormattedTimestamp},");

        var transmissionId2 = DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogTransmission), 2);
        actorCsvData.AppendLine($"{transmissionId2},,1,ActorName,DialogTransmissionSenderActor,,,{transmissionId2},{dto.FormattedTimestamp},{dto.FormattedTimestamp},");

        return actorCsvData.ToString();
    }
}
