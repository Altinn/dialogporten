using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Restore;

public sealed class RestoreDialogEndpointSummary : Summary<RestoreDialogEndpoint>
{
    public RestoreDialogEndpointSummary()
    {
        Summary = "Restore a dialog";
        Description = """
                      Restore a dialog. 
                      """;

        Responses[Status204NoContent] = Constants.SwaggerSummary.Restored.FormatInvariant("aggregate");
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<RestoreDialogEndpoint>().Build();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status409Conflict] = Constants.SwaggerSummary.Conflict;
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
    }

}
