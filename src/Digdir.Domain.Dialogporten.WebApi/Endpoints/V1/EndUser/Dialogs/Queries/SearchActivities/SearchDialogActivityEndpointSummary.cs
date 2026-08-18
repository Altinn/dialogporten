using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchActivities;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.SearchActivities;

public sealed class SearchDialogActivityEndpointSummary : Summary<SearchDialogActivityEndpoint, SearchActivityQuery>
{
    public SearchDialogActivityEndpointSummary()
    {
        Summary = "Gets a list of dialog activities";
        Description = """
                      Gets the list of activities belonging to a dialog
                      """;
        Responses[Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("activity list");
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<SearchDialogActivityEndpoint>();
        Responses[Status403Forbidden] = Constants.SwaggerSummary.AccessDeniedToDialog.FormatInvariant("get");
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
    }
}
