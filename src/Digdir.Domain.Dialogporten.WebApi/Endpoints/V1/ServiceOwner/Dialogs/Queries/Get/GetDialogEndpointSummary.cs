using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.Get;

public sealed class GetDialogEndpointSummary : Summary<GetDialogEndpoint>
{
    public GetDialogEndpointSummary()
    {
        Summary = "Gets a single dialog";
        Description = """
                      Gets a single dialog aggregate.

                      Note that this operation may return deleted dialogs (see the field `DeletedAt`).
                      """;

        Responses[Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("aggregate");
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<GetDialogEndpoint>();
        Responses[Status403Forbidden] =
            Constants.SwaggerSummary.AccessDeniedToDialog.FormatInvariant("get");
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
    }
}
