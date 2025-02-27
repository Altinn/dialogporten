using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Restore;

public sealed class RestoreDialogEndpointSummary : Summary<RestoreDialogEndpoint>
{
    public RestoreDialogEndpointSummary()
    {
        Summary = "Restore a dialog";
        Description = """
                      Restore a dialog. 
                      """;

        Responses[StatusCodes.Status204NoContent] = Constants.SwaggerSummary.Restored.FormatInvariant("aggregate");
        Responses[StatusCodes.Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[StatusCodes.Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
    }

}
