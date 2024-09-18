using AutoMapper;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogActivities.Queries.Get;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DialogActivity, GetDialogActivityDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.TypeId))
            .ForMember(dest => dest.DeletedAt, opt => opt.MapFrom(src => src.Dialog.DeletedAt));

        CreateMap<DialogActivityPerformedByActor, GetDialogActivityPerformedByActorDto>()
            .ForMember(dest => dest.ActorType, opt => opt.MapFrom(src => src.ActorTypeId));

    }
}
