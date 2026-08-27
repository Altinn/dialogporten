using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
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
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<RestoreDialogEndpoint>();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status409Conflict] = Constants.SwaggerSummary.Conflict;
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
    }

}
