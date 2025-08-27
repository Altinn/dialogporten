using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

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
                Guid? actorNameId = activity.TypeId switch
                {
                    DialogActivityType.Values.DialogCreated or
                        DialogActivityType.Values.DialogDeleted or
                        DialogActivityType.Values.DialogRestored or
                        DialogActivityType.Values.Information => null,
                    _ => ActorName.GetRandomId()
                };

                yield return new Actor(
                    Id: activity.Id,
                    CreatedAt: timestamp.Timestamp,
                    UpdatedAt: timestamp.Timestamp,
                    ActorTypeId: actorNameId.HasValue
                        ? ActorType.Values.ServiceOwner
                        : ActorType.Values.PartyRepresentative,
                    Discriminator: "DialogActivityPerformedByActor",
                    TransmissionId: null,
                    DialogSeenLogId: null,
                    ActivityId: activity.Id,
                    LabelAssignmentLogId: null,
                    ActorNameEntityId: actorNameId
                );
            }

            // foreach (var seenLog in DialogSeenLog.GenerateEntities([timestamp]))
            // {
            // Guid? actorNameId = seenLog. switch
            // {
            //     DialogUserType.Values.Person => ActorName.GetRandomId(),
            //     _ => null
            // };

            // yield return new Actor(
            //     Id: seenLog.Id,
            //     CreatedAt: timestamp.Timestamp,
            //     UpdatedAt: timestamp.Timestamp,
            //     ActorTypeId: actorNameId.HasValue
            //         ? ActorType.Values.PartyRepresentative
            //         : ActorType.Values.ServiceOwner,
            //     Discriminator: "DialogSeenLogSeenByActor",
            //     TransmissionId: null,
            //     DialogSeenLogId: seenLog.Id,
            //     ActivityId: null,
            //     LabelAssignmentLogId: null,
            //     ActorNameEntityId: actorNameId
            // );
            // }
        }
    }
}

// internal static class Actor
// {
//     public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
//     {
//         var (dialogPartyActorNameId, transmissionPartyActorNameId) = dto.GetActorNameIds();
//
//         // DialogActivity
//         foreach (var activity in DialogActivity.GenerateEntities([dto]))
//         {
//             var actorNameId = activity.TypeId switch
//             {
//                 DialogActivityType.Values.DialogCreated or
//                 DialogActivityType.Values.DialogDeleted or
//                 DialogActivityType.Values.DialogRestored or
//                 DialogActivityType.Values.Information => string.Empty,
//                 _ => dialogPartyActorNameId.ToString()
//             };
//
//             sb.AppendLine($"{activity.Id},{activity.TypeId},DialogActivityPerformedByActor,{activity.Id},,,{dto.FormattedTimestamp},{dto.FormattedTimestamp},,{actorNameId}");
//         }
//
//         // DialogSeenLog
//         foreach (var seenLog in DialogSeenLog.GetDtos(dto))
//         {
//             var actorNameId = seenLog.EndUserTypeId switch
//             {
//                 DialogUserType.Values.Person => dialogPartyActorNameId.ToString(),
//                 _ => string.Empty
//             };
//
//             sb.AppendLine(
//                 $"{seenLog.Id}," +
//                 $"1," +
//                 $"DialogSeenLogSeenByActor," +
//                 $"{Null}," +
//                 $"{dto.DialogId}," +
//                 $"{dto.FormattedTimestamp}," +
//                 $"{dto.FormattedTimestamp}," +
//                 $"{Null}," +
//                 $"{actorNameId}");
//         }
//
//         // Transmission
//         foreach (var transmission in DialogTransmission.GetDtos(dto))
//         {
//             var actorNameId = transmission.TypeId switch
//             {
//                 DialogTransmissionType.Values.Submission or
//                 DialogTransmissionType.Values.Correction => transmissionPartyActorNameId.ToString(),
//                 _ => string.Empty
//             };
//
//             sb.AppendLine(
//                 $"{transmission.Id}," +
//                 $"1," +
//                 $"DialogTransmissionSenderActor," +
//                 $"{Null}," +
//                 $"{Null}," +
//                 $"{transmission.Id}," +
//                 $"{dto.FormattedTimestamp}," +
//                 $"{dto.FormattedTimestamp}," +
//                 $"{Null}," +
//                 $"{actorNameId}");
//         }
//     });
// }
