using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.Utils;

#pragma warning disable IDE0072

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Actor
{
    public static readonly string CopyCommand = CreateCopyCommand(nameof(Actor),
        "Id", "ActorTypeId", "Discriminator",
        "ActivityId", "DialogSeenLogId", "TransmissionId",
        "CreatedAt", "UpdatedAt", "LabelAssignmentLogId",
        "ActorNameEntityId");

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        var (dialogPartyActorNameId, transmissionPartyActorNameId) = dto.GetActorNameIds();

        // DialogActivity
        foreach (var activity in Activity.GetDtos(dto))
        {
            var actorNameId = activity.TypeId switch
            {
                DialogActivityType.Values.DialogCreated or
                DialogActivityType.Values.DialogDeleted or
                DialogActivityType.Values.DialogRestored or
                DialogActivityType.Values.Information => string.Empty,
                _ => dialogPartyActorNameId.ToString()
            };

            sb.AppendLine($"{activity.Id},{activity.TypeId},DialogActivityPerformedByActor,{activity.Id},,,{dto.FormattedTimestamp},{dto.FormattedTimestamp},,{actorNameId}");
        }

        // DialogSeenLog
        foreach (var seenLog in DialogSeenLog.GetDtos(dto))
        {
            var actorNameId = seenLog.EndUserTypeId switch
            {
                DialogUserType.Values.Person => dialogPartyActorNameId.ToString(),
                _ => string.Empty
            };

            sb.AppendLine(
                $"{seenLog.Id}," +
                $"1," +
                $"DialogSeenLogSeenByActor," +
                $"{Null}," +
                $"{dto.DialogId}," +
                $"{dto.FormattedTimestamp}," +
                $"{dto.FormattedTimestamp}," +
                $"{Null}," +
                $"{actorNameId}");
        }

        // Transmission
        foreach (var transmission in DialogTransmission.GetDtos(dto))
        {
            var actorNameId = transmission.TypeId switch
            {
                DialogTransmissionType.Values.Submission or
                DialogTransmissionType.Values.Correction => transmissionPartyActorNameId.ToString(),
                _ => string.Empty
            };

            sb.AppendLine(
                $"{transmission.Id}," +
                $"1," +
                $"DialogTransmissionSenderActor," +
                $"{Null}," +
                $"{Null}," +
                $"{transmission.Id}," +
                $"{dto.FormattedTimestamp}," +
                $"{dto.FormattedTimestamp}," +
                $"{Null}," +
                $"{actorNameId}");
        }
    });
}
