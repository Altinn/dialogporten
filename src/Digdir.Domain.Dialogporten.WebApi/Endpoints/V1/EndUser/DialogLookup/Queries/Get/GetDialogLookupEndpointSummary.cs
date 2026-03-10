using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using FastEndpoints;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.DialogLookup.Queries.Get;

public sealed class GetDialogLookupEndpointSummary : Summary<GetDialogLookupEndpoint>
{
    public GetDialogLookupEndpointSummary()
    {
        Summary = "Looks up a dialog by instance reference";
        Description = "Resolves dialog metadata and authorization evidence for a supported instance reference.";

        Responses[StatusCodes.Status200OK] = "Successfully resolved identifier lookup metadata.";
        Responses[StatusCodes.Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[StatusCodes.Status401Unauthorized] = Constants.SwaggerSummary.EndUserAuthenticationFailure;
        Responses[StatusCodes.Status403Forbidden] = "Authenticated end user is not authorized for the supplied instance reference.";
        Responses[StatusCodes.Status404NotFound] = "No dialog match was found for the supplied instance reference.";
    }
}
