using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;

internal static class ActorMapper
{
    extension(ActorDto source)
    {
        public DialogActivityPerformedByActor ToDialogActivityPerformedByActor() => new()
        {
            ActorTypeId = source.ActorType,
            ActorNameEntity = source.ToActorName()
        };

        public DialogTransmissionSenderActor ToDialogTransmissionSenderActor() => new()
        {
            ActorTypeId = source.ActorType,
            ActorNameEntity = source.ToActorName()
        };

        public DialogSeenLogSeenByActor ToDialogSeenLogSeenByActor() => new()
        {
            ActorTypeId = source.ActorType,
            ActorNameEntity = source.ToActorName()
        };

        public LabelAssignmentLogActor ToLabelAssignmentLogActor() => new()
        {
            ActorTypeId = source.ActorType,
            ActorNameEntity = source.ToActorName()
        };

        private ActorName? ToActorName() => source.ActorName is not null || source.ActorId is not null
            ? new ActorName
            {
                Name = source.ActorName,
                ActorId = source.ActorId
            }
            : null;
    }

    extension(Actor source)
    {
        public ActorDto ToDto() => new()
        {
            ActorType = source.ActorTypeId,
            ActorName = source.ActorNameEntity?.Name,
            ActorId = source.ActorNameEntity?.ActorId
        };
    }
}
