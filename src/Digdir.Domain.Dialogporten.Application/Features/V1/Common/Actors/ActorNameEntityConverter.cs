using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Actors;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Actors;

internal abstract class ActorNameEntityConverter : ITypeConverter<ActorDto, ActorName?>
{
    public ActorName Convert(ActorDto source, ActorName? destination, ResolutionContext context)
    {
        destination ??= new ActorName();

        destination.Name = source.ActorName;
        destination.ActorId = source.ActorId;

        return destination;
    }
}

internal abstract class ActorNameValueConverter : IValueConverter<ActorDto, ActorName>
{
    public ActorName Convert(ActorDto sourceMember, ResolutionContext context)
    {
        var actorNameEntity = new ActorName
        {
            ActorId = sourceMember.ActorId,
            Name = sourceMember.ActorName
        };
        return actorNameEntity;
    }
}

internal sealed class ActorConverter : ITypeConverter<ActorDto?, ActorName?>
{

    public ActorName? Convert(ActorDto? source, ActorName? destination, ResolutionContext context)
    {
        if (source == null)
        {
            return null;
        }

        destination ??= new ActorName();
        destination.ActorId = source.ActorId;
        destination.Name = source.ActorName;
        return destination;
    }
}
