using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLogs;

public sealed class SearchDialogLabelAssignmentLogEndpointSummary : Summary<SearchDialogLabelAssignmentLogEndpoint, SearchLabelAssignmentLogQuery>
{
    public SearchDialogLabelAssignmentLogEndpointSummary()
    {
        Summary = "Gets a list of dialog label assignment logs";
        Description = """
                      Gets the list of label assignment logs belonging to a dialog
                      """;

        Responses[Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("label assignment log list");
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<SearchDialogLabelAssignmentLogEndpoint>();
        Responses[Status403Forbidden] = Constants.SwaggerSummary.AccessDeniedToDialog.FormatInvariant("get");
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
    }
}
