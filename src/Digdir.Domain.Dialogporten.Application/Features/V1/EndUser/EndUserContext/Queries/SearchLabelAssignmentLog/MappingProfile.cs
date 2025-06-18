using AutoMapper;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<LabelAssignmentLog, LabelAssignmentLogDto>();
    }
}
