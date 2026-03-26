using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<LabelAssignmentLog, LabelAssignmentLogDto>()
            .ForMember(dest => dest.PerformedBy, opt => opt.MapFrom(src => src.PerformedBy.ToDto()));
    }
}
