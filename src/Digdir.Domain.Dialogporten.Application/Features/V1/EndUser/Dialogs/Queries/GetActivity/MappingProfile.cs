using AutoMapper;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetActivity;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DialogActivity, ActivityDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.TypeId));

    }
}
