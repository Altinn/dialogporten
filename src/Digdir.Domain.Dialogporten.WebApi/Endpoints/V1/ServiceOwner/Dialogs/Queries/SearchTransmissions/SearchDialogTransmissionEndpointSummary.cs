using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchTransmissions;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.SearchTransmissions;

public sealed class SearchDialogTransmissionEndpointSummary : Summary<SearchDialogTransmissionEndpoint, SearchTransmissionQuery>
{
    public SearchDialogTransmissionEndpointSummary()
    {
        Summary = "Gets a list of dialog transmissions";
        Description = """
                      Gets the list of transmissions belonging to a dialog
                      """;
        Responses[Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("transmission list");
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<SearchDialogTransmissionEndpoint>();
        Responses[Status403Forbidden] = Constants.SwaggerSummary.AccessDeniedToDialog.FormatInvariant("get");
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
    }
}
