using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.EndUserContext.Commands.SetSystemLabel;

public sealed class SetDialogSystemLabelsEndpointSummary : Summary<SetDialogSystemLabelsEndpoint>
{
    public SetDialogSystemLabelsEndpointSummary()
    {
        Summary = "Sets the system labels of a dialog";
        Description = $"""
                       Sets the system labels of the dialog.

                       {Constants.SwaggerSummary.OptimisticConcurrencyNoteEnduserContext}
                       """;

        ResponseHeaders = [HttpResponseHeaderExamples.NewDialogETagHeader(Status204NoContent)];
        Responses[Status204NoContent] = Constants.SwaggerSummary.Updated.FormatInvariant("system label");
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<SetDialogSystemLabelsEndpoint>();
        Responses[Status403Forbidden] = Constants.SwaggerSummary.AccessDeniedToDialog.FormatInvariant("update");
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status409Conflict] = Constants.SwaggerSummary.Conflict;
        Responses[Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
        Responses[Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
