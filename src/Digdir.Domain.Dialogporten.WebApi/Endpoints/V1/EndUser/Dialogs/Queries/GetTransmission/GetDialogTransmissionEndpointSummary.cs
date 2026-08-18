using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.GetTransmission;

public sealed class GetDialogTransmissionEndpointSummary : Summary<GetDialogTransmissionEndpoint>
{
    public GetDialogTransmissionEndpointSummary()
    {
        Summary = "Gets a single dialog transmission";
        Description = """
                      Gets a single transmission belonging to a dialog.
                      """;
        Responses[Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("transmission");
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<GetDialogTransmissionEndpoint>();
        Responses[Status403Forbidden] = Constants.SwaggerSummary.AccessDeniedToDialogForChildEntity.FormatInvariant("get");
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogTransmissionNotFound;
        Responses[Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
    }
}
