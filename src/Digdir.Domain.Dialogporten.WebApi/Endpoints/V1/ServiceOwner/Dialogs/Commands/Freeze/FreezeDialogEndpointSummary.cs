using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Freeze;

public sealed class FreezeDialogEndpointSummary : Summary<FreezeDialogEndpoint>
{
    public FreezeDialogEndpointSummary()
    {
        Summary = "Freezes a dialog";
        Description = """
                      Freezes a given dialog
                      
                      the dialog cannot be updated/deleted by the service owner (but can still be altered via admin-scope) when frozen
                      """;
        Responses[StatusCodes.Status204NoContent] = Constants.SwaggerSummary.Frozen.FormatInvariant("aggregate");
    }

}
