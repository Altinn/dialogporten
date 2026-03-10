using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.DialogLookup.Queries.Get;

public sealed class GetDialogLookupEndpointSummary : Summary<GetDialogLookupEndpoint>
{
    public GetDialogLookupEndpointSummary()
    {
        Summary = "Looks up a dialog by instance reference";
        Description = "Resolves dialog metadata for a supported instance reference in service owner context.";

        Responses[StatusCodes.Status200OK] = "Successfully resolved identifier lookup metadata.";
        Responses[StatusCodes.Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[StatusCodes.Status401Unauthorized] =
            Constants.SwaggerSummary.ServiceOwnerAuthenticationFailure.FormatInvariant(AuthorizationScope.ServiceProvider);
        Responses[StatusCodes.Status403Forbidden] = "Authenticated service owner does not own the resolved dialog.";
        Responses[StatusCodes.Status404NotFound] = "No dialog match was found for the supplied instance reference.";
    }
}
