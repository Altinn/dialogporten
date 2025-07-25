using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.EndUserContext.Commands.SetSystemLabel;

public sealed class SetDialogSystemLabelsEndpointSummary : Summary<SetDialogSystemLabelsEndpoint>
{
    public SetDialogSystemLabelsEndpointSummary()
    {
        Summary = "Sets the system labels of a dialog";
        Description = $"""
                       Sets the system labels of the dialog.

                       {Constants.SwaggerSummary.OptimisticConcurrencyNoteEnduserContext}
                       """;

        ResponseHeaders = [HttpResponseHeaderExamples.NewDialogETagHeader(StatusCodes.Status204NoContent)];
        Responses[StatusCodes.Status204NoContent] = Constants.SwaggerSummary.Updated.FormatInvariant("system label");
        Responses[StatusCodes.Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[StatusCodes.Status401Unauthorized] = Constants.SwaggerSummary.EndUserAuthenticationFailure;
        Responses[StatusCodes.Status403Forbidden] = Constants.SwaggerSummary.AccessDeniedToDialog.FormatInvariant("update");
        Responses[StatusCodes.Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[StatusCodes.Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
        Responses[StatusCodes.Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
        Responses[StatusCodes.Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
