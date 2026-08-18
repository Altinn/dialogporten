using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.DialogLookup.Queries.Get;

public sealed class GetDialogLookupEndpointSummary : Summary<GetDialogLookupEndpoint>
{
    public GetDialogLookupEndpointSummary()
    {
        Summary = "Looks up a dialog by instance reference";
        Description = "Resolves dialog metadata for a supported instance reference in service owner context.";

        Responses[Status200OK] = "Successfully resolved instance reference lookup metadata.";
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<GetDialogLookupEndpoint>();
        Responses[Status403Forbidden] = "Authenticated service owner does not own the resolved dialog.";
        Responses[Status404NotFound] = "No dialog match was found for the supplied instance reference.";
    }
}
