using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;
using MediatR;
using static Digdir.Domain.Dialogporten.GraphQL.Common.Constants;
using ServiceResourceMetadataModel = Digdir.Domain.Dialogporten.GraphQL.EndUser.ServiceResourceMetadata.ServiceResourceMetadata;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public partial class Queries
{
    public async Task<ServiceResourceMetadataModel> GetServiceResources(
        [Service] ISender mediator,
        [Service] IMapper mapper,
        [GlobalState(AcceptLanguage)] AcceptedLanguages? acceptLanguage,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetServiceResourceMetadataQuery
        {
            AcceptedLanguages = acceptLanguage?.AcceptedLanguage
        }, cancellationToken);

        return mapper.Map<ServiceResourceMetadataModel>(result);
    }
}
