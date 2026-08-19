using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.Get;

public sealed class GetDialogEndpointSummary : Summary<GetDialogEndpoint>
{
    public GetDialogEndpointSummary()
    {
        Summary = "Gets a single dialog";
        Description = """
                      Gets a single dialog aggregate.
                      """;

        Responses[Status200OK] =
            Constants.SwaggerSummary.ReturnedResult.FormatInvariant("aggregate");
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<GetDialogEndpoint>()
            .Or(Constants.SwaggerSummary.AccessDeniedToDialog.FormatInvariant("get"))
            .Build();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
    }
}
