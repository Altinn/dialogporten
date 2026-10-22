using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.DeleteRelation;

public sealed class DeleteDialogRelationEndpointSummary : Summary<DeleteDialogRelationEndpoint>
{
    public DeleteDialogRelationEndpointSummary()
    {
        Summary = "Removes a relation to another dialog";
        Description = """
                      Removes the relation between the dialog in the path and the related dialog. The owner of either dialog may remove it.

                      Only the dialog in the path is updated (new revision and a dialog.updated event). The related dialog only gets its hasRelations flag cleared if this was its last relation.
                      """;
        ResponseHeaders = [HttpResponseHeaderExamples.NewDialogETagHeader(Status204NoContent)];
        Responses[Status204NoContent] = "The relation was removed.";
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<DeleteDialogRelationEndpoint>().Build();
        Responses[Status404NotFound] = "The dialog, or the relation, was not found.";
        Responses[Status409Conflict] = new ConflictMessageBuilder()
            .ConcurrentOperationRejected()
            .Build();
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
        Responses[Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
