using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using DialogActivityDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogActivityDto;
using DialogSeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogSeenLogDto;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<LocalizationDto, Localization>();

        CreateMap<ContentValueDto, ContentValue>();

        CreateMap<DialogSeenLogDto, SeenLog>();
        CreateMap<ActorDto, Actor>();

        CreateMap<DialogSeenLogDto, SeenLog>();
        CreateMap<ActorDto, Actor>();

        CreateMap<Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogActivityDto, Activity>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type));
        CreateMap<ActorDto, Actor>()
            .ForMember(dest => dest.ActorType, opt => opt.MapFrom(src => src.ActorType));

        CreateMap<DialogActivityDto, Activity>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type));
        CreateMap<ActorDto, Actor>()
            .ForMember(dest => dest.ActorType, opt => opt.MapFrom(src => src.ActorType));
    }
}
