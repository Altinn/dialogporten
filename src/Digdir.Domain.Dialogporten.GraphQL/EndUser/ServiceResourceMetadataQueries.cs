using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;
using MediatR;
using static Digdir.Domain.Dialogporten.GraphQL.Common.Constants;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public partial class Queries
{
    public async Task<GetServiceResourceMetadataDto> GetServiceResources(
        [Service] ISender mediator,
        [GlobalState(AcceptLanguage)] AcceptedLanguages? acceptLanguage,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetServiceResourceMetadataQuery
        {
            AcceptedLanguages = acceptLanguage?.AcceptedLanguage
        }, cancellationToken);
    }

    public async Task<SearchAuthorizedServiceResourcesDto> SearchServiceResources(
        [Service] ISender mediator,
        [GlobalState(AcceptLanguage)] AcceptedLanguages? acceptLanguage,
        string[]? parties = null,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new SearchAuthorizedServiceResourcesQuery
        {
            AcceptedLanguages = acceptLanguage?.AcceptedLanguage,
            Parties = parties
        }, cancellationToken);
    }
}
