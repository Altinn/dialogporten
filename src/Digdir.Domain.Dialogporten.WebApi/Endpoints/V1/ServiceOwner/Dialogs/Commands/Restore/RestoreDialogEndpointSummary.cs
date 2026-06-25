using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
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

        Responses[StatusCodes.Status204NoContent] = Constants.SwaggerSummary.Restored.FormatInvariant("aggregate");
        Responses[StatusCodes.Status401Unauthorized] = Constants
            .SwaggerSummary
            .ServiceOwnerAuthenticationFailure
            .FormatInvariant(AuthorizationScope.ServiceProvider);
        Responses[StatusCodes.Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[StatusCodes.Status409Conflict] = Constants.SwaggerSummary.Conflict;
        Responses[StatusCodes.Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
    }

}
