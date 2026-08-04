using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.SearchTerms.Queries.Get;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchTerms;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<SearchTermsDto, SearchTerms>();
        CreateMap<SearchTermDto, SearchTerm>();
    }
}
