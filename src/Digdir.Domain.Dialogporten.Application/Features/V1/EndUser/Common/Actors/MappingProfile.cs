using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Actors;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {

        var actorDtoType = typeof(ActorDto);
        var actorType = typeof(Actor);

        var derivedActorTypes = DomainAssemblyMarker
            .Assembly
            .GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(actorType))
            .ToList();

        CreateMap<Actor, ActorDto>().IncludeMembers(src => src.ActorNameEntity)
            .ForMember(dest => dest.ActorType, opt => opt.MapFrom(src => src.ActorTypeId));

        CreateMap<ActorName, ActorDto>()
            .ForMember(dest => dest.ActorName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ActorId, opt => opt.MapFrom(src => IdentifierMasker.GetMaybeMaskedIdentifier(src.ActorId)));
        // CreateMap<ActorName, ActorDto>()
        //     .ConvertUsing<ActorNameEntityConverter>();

        foreach (var outputActor in derivedActorTypes)
        {
            CreateMap(outputActor, actorDtoType)
                .IncludeBase(actorType, actorDtoType);
        }
    }
}

internal abstract class ActorNameEntityConverter : ITypeConverter<ActorName, ActorDto>
{
    public ActorDto Convert(ActorName source, ActorDto destination, ResolutionContext context)
    {
        destination.ActorName = source.Name;
        destination.ActorId = IdentifierMasker.GetMaybeMaskedIdentifier(source.ActorId);

        return destination;
    }
}
