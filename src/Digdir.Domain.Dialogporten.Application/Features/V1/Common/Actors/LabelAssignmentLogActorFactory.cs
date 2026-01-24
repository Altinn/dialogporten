using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Actors;

internal static class LabelAssignmentLogActorFactory
{
    public static LabelAssignmentLogActor FromUserInformation(UserInformation userInformation) =>
        Create(ActorType.Values.PartyRepresentative, userInformation.UserId.ExternalIdWithPrefix, userInformation.Name);

    public static LabelAssignmentLogActor Create(ActorType.Values actorType, string? actorId, string? actorName)
    {
        var actor = new LabelAssignmentLogActor
        {
            ActorTypeId = actorType
        };

        if (actorType == ActorType.Values.PartyRepresentative && (actorId is not null || actorName is not null))
        {
            actor.ActorNameEntity = new ActorName
            {
                ActorId = actorId,
                Name = actorName
            };
        }

        return actor;
    }
}
