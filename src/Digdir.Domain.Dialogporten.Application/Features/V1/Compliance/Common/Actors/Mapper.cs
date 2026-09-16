using Digdir.Domain.Dialogporten.Domain.Actors;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Common.Actors;

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
