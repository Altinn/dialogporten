using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels;

public sealed class BulkSetDialogSystemLabelsEndpointSummary : Summary<BulkSetDialogSystemLabelsEndpoint>
{
    public BulkSetDialogSystemLabelsEndpointSummary()
    {
        Summary = "Sets system labels for multiple dialogs";
        Description = """
                      Sets the system labels for a list of dialogs, optionally including a end user context revision for each dialog.
                      """;

        Responses[Status204NoContent] = Constants.SwaggerSummary.Updated.FormatInvariant("system labels");
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<BulkSetDialogSystemLabelsEndpoint>();
        Responses[Status403Forbidden] = Constants.SwaggerSummary.AccessDeniedToDialog.FormatInvariant("update");
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status409Conflict] = Constants.SwaggerSummary.Conflict;
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
        Responses[Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
