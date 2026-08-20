using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Purge;

public sealed class PurgeDialogEndpointSummary : Summary<PurgeDialogEndpoint>
{
    public PurgeDialogEndpointSummary()
    {
        Summary = "Permanently deletes a dialog";
        Description = """
                      Deletes a given dialog (hard delete).

                      Optimistic concurrency control is implemented using the If-Match header. Supply the Revision value from the GetDialog endpoint to ensure that the dialog is not deleted by another request in the meantime.
                      """;

        Responses[Status204NoContent] = Constants.SwaggerSummary.Deleted.FormatInvariant("aggregate");
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<PurgeDialogEndpoint>()
            .Or(Constants.SwaggerSummary.AccessDeniedToDialog.FormatInvariant("delete"))
            .Build();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status409Conflict] = Constants.SwaggerSummary.Conflict;
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
    }
}
