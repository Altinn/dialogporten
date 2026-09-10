using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
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
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<GetDialogTransmissionEndpoint>()
            .Or(Constants.SwaggerSummary.AccessDeniedToDialogForChildEntity.FormatInvariant("get"))
            .Build();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogTransmissionNotFound;
        Responses[Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
    }
}
