using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchEndUserContext;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.SearchEndUserContext;

public sealed class SearchDialogEndUserContextEndpointSummary : Summary<SearchDialogEndUserContextEndpoint, SearchDialogEndUserContextQuery>
{
    public SearchDialogEndUserContextEndpointSummary()
    {
        Summary = "Gets end user context system labels for dialogs";
        Description = """
                      Performs a search for dialog end user context labels, returning a paginated list of dialogs.

                      * Party is required.
                      * System labels are matched with OR semantics.
                      * See \"continuationToken\" in the response for how to get the next page of results.
                      * hasNextPage will be set to true if there are more items to get.
                      """;

        Responses[StatusCodes.Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("list");
        Responses[StatusCodes.Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[StatusCodes.Status401Unauthorized] =
            Constants.SwaggerSummary.ServiceOwnerAuthenticationFailure.FormatInvariant(AuthorizationScope
                .ServiceProviderSearch);

        RequestParam(p => p.ContinuationToken,
            "Supply \"continuationToken\" for the response to get the next page of results, if hasNextPage is true");
        RequestParam(p => p.Limit,
            $"Limit the number of results per page ({PaginationConstants.MinLimit}-{PaginationConstants.MaxLimit}, default: {PaginationConstants.DefaultLimit})");
    }
}
