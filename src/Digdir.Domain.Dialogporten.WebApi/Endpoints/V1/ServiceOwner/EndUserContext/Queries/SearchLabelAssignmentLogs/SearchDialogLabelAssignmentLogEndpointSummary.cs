using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.EndUserContext.Queries.SearchLabelAssignmentLogs;

public sealed class SearchDialogLabelAssignmentLogEndpointSummary : Summary<SearchDialogLabelAssignmentLogEndpoint, SearchDialogLabelAssignmentLogRequest>
{
    public SearchDialogLabelAssignmentLogEndpointSummary()
    {
        Summary = "Gets a list of dialog label assignment logs";
        Description = """
                      Gets the list of label assignment logs belonging to a dialog.

                      If enduserId is supplied, the end user must have access to the dialog.
                      """;

        Responses[Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("label assignment log list");
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<SearchDialogLabelAssignmentLogEndpoint>().Build();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
    }
}
