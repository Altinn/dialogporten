using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.Search;

public sealed class ListDialogEndpointSummary : Summary<SearchDialogEndpoint, SearchDialogQuery>
{
    public ListDialogEndpointSummary()
    {
        Summary = "Gets a list of dialogs";
        Description = """
                      Performs a search for dialogs, returning a paginated list of dialogs.

                      * All date parameters must contain explicit time zone. Example: 2023-10-27T10:00:00Z or 2023-10-27T10:00:00+01:00
                      * See "continuationToken" in the response for how to get the next page of results.
                      * hasNextPage will be set to true if there are more items to get.
                      """;

        Responses[StatusCodes.Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("list");
        Responses[StatusCodes.Status401Unauthorized] =
            Constants.SwaggerSummary.ServiceOwnerAuthenticationFailure.FormatInvariant(AuthorizationScope
                .ServiceProviderSearch);

        RequestParam(p => p.ContinuationToken,
            "Supply \"continuationToken\" for the response to get the next page of results, if hasNextPage is true");
        RequestParam(p => p.Limit,
            $"Limit the number of results per page ({PaginationConstants.MinLimit}-{PaginationConstants.MaxLimit}, default: {PaginationConstants.DefaultLimit})");
    }
}
