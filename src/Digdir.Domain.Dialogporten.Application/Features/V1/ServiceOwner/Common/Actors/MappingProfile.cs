using AutoMapper;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Actors;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;

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

        CreateMap<ActorDto, Actor>()
            .ForMember(dest => dest.ActorType, opt => opt.Ignore())
            .ForMember(dest => dest.ActorTypeId, opt => opt.MapFrom(src => src.ActorType))
            .ForMember(dest => dest.ActorNameEntity, opt =>
            {
                opt.Condition(x => x.ActorName is not null || x.ActorId is not null);
                opt.MapFrom(src => src);
            });

        CreateMap<ActorDto, ActorName>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ActorName))
            .ForMember(dest => dest.ActorId, opt => opt.MapFrom(src => src.ActorId));

        foreach (var inputActor in derivedActorTypes)
        {
            CreateMap(actorDtoType, inputActor)
                .IncludeBase(actorDtoType, actorType);
        }

        CreateMap<Actor, ActorDto>()
            .IncludeMembers(src => src.ActorNameEntity)
            .ForMember(dest => dest.ActorType, opt => opt.MapFrom(src => src.ActorTypeId));

        CreateMap<ActorName, ActorDto>()
            .ForMember(dest => dest.ActorName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ActorId, opt => opt.MapFrom(src => src.ActorId));

        foreach (var outputActor in derivedActorTypes)
        {
            CreateMap(outputActor, actorDtoType)
                .IncludeBase(actorType, actorDtoType);
        }
    }
}
