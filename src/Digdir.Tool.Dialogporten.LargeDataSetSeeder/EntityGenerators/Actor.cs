using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

#pragma warning disable IDE0072

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

// public abstract class Actor : IEntity
// {
//     public Guid Id { get; set; }
//     public DateTimeOffset CreatedAt { get; set; }
//     public DateTimeOffset UpdatedAt { get; set; }
//     public ActorType.Values ActorTypeId { get; set; }
//     public ActorType ActorType { get; set; } = null!;
//
//     public Guid? ActorNameEntityId { get; set; }
//     public ActorName? ActorNameEntity { get; set; } = null!;
// }

public sealed record Actor(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    ActorType.Values ActorTypeId,
    string Discriminator,
    Guid? ActorNameEntityId
    ) : IEntityGenerator<Actor>
{
    public static IEnumerable<Actor> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        return [];
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
