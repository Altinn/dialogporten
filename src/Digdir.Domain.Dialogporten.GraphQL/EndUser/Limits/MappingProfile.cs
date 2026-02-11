using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.Limits.Queries.Get;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Limits;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<GetLimitsDto, Limits>();
        CreateMap<EndUserSearchLimitsDto, EndUserSearchLimits>();
        CreateMap<ServiceOwnerSearchLimitsDto, ServiceOwnerSearchLimits>();
    }
}
