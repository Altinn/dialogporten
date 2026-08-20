using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.SearchTerms.Queries.Get;
using Digdir.Domain.Dialogporten.Domain.SearchTerms;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchTerms;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<SearchTermsDto, SearchTerms>();
        CreateMap<SearchTermEntry, SearchTerm>();
    }
}
