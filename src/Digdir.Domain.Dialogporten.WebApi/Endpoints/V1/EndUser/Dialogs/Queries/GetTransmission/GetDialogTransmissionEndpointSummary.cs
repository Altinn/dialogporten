using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
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
        Responses[StatusCodes.Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("transmission");
        Responses[StatusCodes.Status401Unauthorized] = Constants.SwaggerSummary.EndUserAuthenticationFailure.FormatInvariant(AuthorizationScope.EndUser);
        Responses[StatusCodes.Status403Forbidden] = Constants.SwaggerSummary.AccessDeniedToDialogForChildEntity.FormatInvariant("get");
        Responses[StatusCodes.Status404NotFound] = Constants.SwaggerSummary.DialogTransmissionNotFound;
        Responses[StatusCodes.Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
    }
}
