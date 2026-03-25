using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateActivityDto, DialogActivity>()
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.TypeId, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.PerformedBy, opt => opt.MapFrom(src => src.PerformedBy.ToActor<DialogActivityPerformedByActor>()));
    }
}
