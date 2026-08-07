using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.SearchTerms.Queries.Get;
using MediatR;
using static Digdir.Domain.Dialogporten.GraphQL.Common.Constants;
using SearchTermsModel = Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchTerms.SearchTerms;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public partial class Queries
{
    // Intentionally NOT compressed (unlike GetServiceResources): the WebApi endpoint owns compression
    // for this payload. Returns null when no search-term list has been generated yet.
    public async Task<SearchTermsModel?> GetSearchTerms(
        [Service] ISender mediator,
        [Service] IMapper mapper,
        [GlobalState(AcceptLanguage)] AcceptedLanguages? acceptLanguage,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSearchTermsQuery
        {
            AcceptedLanguages = acceptLanguage?.AcceptedLanguage
        }, cancellationToken);

        return result.Match<SearchTermsModel?>(
            mapper.Map<SearchTermsModel>,
            _ => null);
    }
}
