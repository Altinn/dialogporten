using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;
using MediatR;
using static Digdir.Domain.Dialogporten.GraphQL.Common.Constants;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public partial class Queries
{
    public async Task<GetServiceResourceMetadataDto> GetServiceResources(
        [Service] ISender mediator,
        [GlobalState(AcceptLanguage)] AcceptedLanguages? acceptLanguage,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetServiceResourceMetadataQuery
        {
            AcceptedLanguages = acceptLanguage?.AcceptedLanguage
        }, cancellationToken);
    }
}
