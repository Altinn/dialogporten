using AutoMapper;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchSeenLogs;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DialogSeenLog, SeenLogDto>()
            .ForMember(dest => dest.SeenAt, opt => opt.MapFrom(src => src.CreatedAt));
    }
}
