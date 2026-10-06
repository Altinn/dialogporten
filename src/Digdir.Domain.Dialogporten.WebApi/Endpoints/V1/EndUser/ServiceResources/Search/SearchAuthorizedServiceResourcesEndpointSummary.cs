using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

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
        Responses[Status200OK] = "Authorized service resource metadata.";
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<SearchAuthorizedServiceResourcesEndpoint>().Build();
    }
}
