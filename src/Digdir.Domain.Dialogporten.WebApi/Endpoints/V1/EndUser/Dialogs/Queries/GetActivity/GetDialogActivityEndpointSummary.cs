using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.GetActivity;

public sealed class GetDialogActivityEndpointSummary : Summary<GetDialogActivityEndpoint>
{
    public GetDialogActivityEndpointSummary()
    {
        Summary = "Gets a single dialog activity";
        Description = """
                      Gets a single activity belonging to a dialog.
                      """;
        Responses[Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("activity");
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<GetDialogActivityEndpoint>()
            .Or(Constants.SwaggerSummary.AccessDeniedToDialogForChildEntity.FormatInvariant("get"))
            .Build();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogActivityNotFound;
    }
}
