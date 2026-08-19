using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Delete;

public sealed class DeleteDialogEndpointSummary : Summary<DeleteDialogEndpoint>
{
    public DeleteDialogEndpointSummary()
    {
        Summary = "Deletes a dialog";
        Description = """
                      Deletes a given dialog (soft delete).

                      Note that the dialog will still be available on the single details endpoint, but will have a deleted status. It will not appear on the list endpoint for either service owners nor end users.
                      If end users attempt to access the dialog via the details endpoint, they will get a 410 Gone response.

                      Optimistic concurrency control is implemented using the If-Match header. Supply the Revision value from the GetDialog endpoint to ensure that the dialog is not deleted by another request in the meantime.
                      """;
        ResponseHeaders = [HttpResponseHeaderExamples.NewDialogETagHeader(Status204NoContent)];
        Responses[Status204NoContent] = Constants.SwaggerSummary.Deleted.FormatInvariant("aggregate");
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<DeleteDialogEndpoint>()
            .Or(Constants.SwaggerSummary.AccessDeniedToDialog.FormatInvariant("delete"))
            .Build();
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status409Conflict] = Constants.SwaggerSummary.Conflict;
        Responses[Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
    }
}
