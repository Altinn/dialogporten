using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;

internal static class ActorMapper
{
    extension(Actor source)
    {
        public ActorDto ToDto() => new()
        {
            ActorType = source.ActorTypeId,
            ActorName = source.ActorNameEntity?.Name,
            ActorId = IdentifierMasker.GetMaybeMaskedIdentifier(source.ActorNameEntity?.ActorId)
        };
    }
}
