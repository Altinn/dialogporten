using Digdir.Domain.Dialogporten.Domain.Actors;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;

internal static class ActorMapExtensions
{
    extension(Actor source)
    {
        internal ActorDto ToDto() => new()
        {
            ActorType = source.ActorTypeId,
            ActorName = source.ActorNameEntity?.Name,
            ActorId = source.ActorNameEntity?.ActorId
        };
    }
}

internal static class ActorDtoMapExtensions
{
    extension(ActorDto source)
    {
        internal T ToActor<T>() where T : Actor, new() => new()
        {
            ActorTypeId = source.ActorType,
            ActorNameEntity = source.ActorName is not null || source.ActorId is not null
                ? new ActorName { Name = source.ActorName, ActorId = source.ActorId }
                : null
        };
    }
}
