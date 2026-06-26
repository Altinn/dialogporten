using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.ServiceResourceMetadata;
using MediatR;
using Microsoft.Extensions.Options;
using static Digdir.Domain.Dialogporten.GraphQL.Common.Constants;
using ServiceResourceMetadataModel = Digdir.Domain.Dialogporten.GraphQL.EndUser.ServiceResourceMetadata.ServiceResourceMetadata;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public partial class Queries
{
    public async Task<ServiceResourceMetadataModel> GetServiceResources(
        [Service] ISender mediator,
        [Service] IOptionsSnapshot<ApplicationSettings> applicationSettings,
        [GlobalState(AcceptLanguage)] AcceptedLanguages? acceptLanguage,
        string[]? parties = null,
        bool includeUnauthorized = false,
        CancellationToken cancellationToken = default)
    {
        if (includeUnauthorized || !applicationSettings.Value.FeatureToggle.EnableGraphQlAuthorizedServiceResources)
        {
            // Full public catalogue: returns every referenced resource regardless of the caller's
            // authorizations. Parties filter is ignored on this branch.
            var publicResult = await mediator.Send(new GetServiceResourceMetadataQuery
            {
                AcceptedLanguages = acceptLanguage?.AcceptedLanguage
            }, cancellationToken);

            return publicResult.ToServiceResourceMetadata();
        }

        // Authorized branch: filtered to the caller's resources.
        var result = await mediator.Send(new SearchAuthorizedServiceResourcesQuery
        {
            AcceptedLanguages = acceptLanguage?.AcceptedLanguage,
            Parties = parties
        }, cancellationToken);

        return result.ToServiceResourceMetadata();
    }
}
