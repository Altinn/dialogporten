using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.SearchTerms.Queries.Get;
using MediatR;
using static Digdir.Domain.Dialogporten.GraphQL.Common.Constants;
using SearchTermsModel = Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchTerms.SearchTerms;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public partial class Queries
{
    // Served uncompressed: the GraphQL host has no response compression at all — per-resolver
    // opt-in was removed in #4113 because one compressed response can span multiple fields,
    // reintroducing CRIME/BREACH. Clients wanting compression (and ETag/304 revalidation)
    // should use the WebApi endpoint, which owns those concerns for this payload.
    // Returns null when no search-term list has been generated yet.
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
