using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels;

public sealed class BulkSetDialogSystemLabelsEndpointSummary : Summary<BulkSetDialogSystemLabelsEndpoint>
{
    public BulkSetDialogSystemLabelsEndpointSummary()
    {
        Summary = "Sets system labels for multiple dialogs";
        Description = """
                      Sets the system labels for a list of dialogs, optionally including a end user context revision for each dialog.
                      """;

        Responses[StatusCodes.Status204NoContent] = Constants.SwaggerSummary.Updated.FormatInvariant("system labels");
        Responses[StatusCodes.Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[StatusCodes.Status401Unauthorized] = Constants.SwaggerSummary.EndUserAuthenticationFailure;
        Responses[StatusCodes.Status403Forbidden] = Constants.SwaggerSummary.AccessDeniedToDialog.FormatInvariant("update");
        Responses[StatusCodes.Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
        Responses[StatusCodes.Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
