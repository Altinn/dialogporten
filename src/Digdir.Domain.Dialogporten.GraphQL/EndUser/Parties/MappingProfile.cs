using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Parties;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<AuthorizedPartyDto, AuthorizedParty>();
        CreateMap<AuthorizedPartyDto, AuthorizedSubParty>();
    }
}
