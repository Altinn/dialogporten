using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.ServiceResources.Search;

public sealed class SearchAuthorizedServiceResourcesEndpointSummary : Summary<SearchAuthorizedServiceResourcesEndpoint>
{
    public SearchAuthorizedServiceResourcesEndpointSummary()
    {
        Summary = "Gets the service resources the authenticated end user is authorized to use.";
        Description = "Returns the same service resource metadata as the public metadata endpoint, filtered to the " +
                      "resources the calling end user is authorized to use. Optionally narrowed by one or more party URNs. " +
                      "For callers authorized to a very large number of parties on an unfiltered request, the full referenced " +
                      "catalogue is returned instead of the authorized subset, signalled by 'isFullCatalogueFallback' " +
                      "(supply a party filter to always get an authorization-scoped result).";
        Responses[StatusCodes.Status200OK] = "Authorized service resource metadata.";
    }
}
