using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.DialogLookup.Queries.Get;

public sealed class GetDialogLookupEndpointSummary : Summary<GetDialogLookupEndpoint>
{
    public GetDialogLookupEndpointSummary()
    {
        Summary = "Looks up a dialog by instance reference";
        Description = "Resolves dialog metadata and authorization evidence for a supported instance reference.";

        Responses[Status200OK] = "Successfully resolved dialog lookup metadata.";
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<GetDialogLookupEndpoint>();
        Responses[Status403Forbidden] = "Authenticated end user is not authorized for the supplied instance reference.";
        Responses[Status404NotFound] = "No dialog match was found for the supplied instance reference.";
    }
}
