using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.CreateRelation;

public sealed class CreateDialogRelationEndpointSummary : Summary<CreateDialogRelationEndpoint>
{
    public CreateDialogRelationEndpointSummary()
    {
        Summary = "Adds a relation to another dialog";
        Description = """
                      Relates the dialog to another existing dialog, which may belong to another party or service owner.
                      The type is seen from the dialog in the path: "Parent" makes the related dialog the parent of this dialog, "Child" makes it a child, and "Related" is a symmetric relation.

                      A dialog has at most one parent, parent/child hierarchies have a limited depth, a pair of dialogs can only have one relation, and a dialog can have a limited number of relations in total.

                      Only this dialog is updated (new revision and a dialog.updated event). The related dialog only gets its hasRelations flag set.

                      Optimistic concurrency control is implemented using the If-Match header. Supply the Revision value from the GetDialog endpoint to ensure that the dialog is not modified/deleted by another request in the meantime.
                      """;
        ResponseHeaders = [HttpResponseHeaderExamples.NewDialogETagHeader(Status204NoContent)];
        Responses[Status204NoContent] = "The relation was added.";
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<CreateDialogRelationEndpoint>().Build();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status409Conflict] = new ConflictMessageBuilder()
            .ConcurrentOperationRejected()
            .Build();
        Responses[Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
        Responses[Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
