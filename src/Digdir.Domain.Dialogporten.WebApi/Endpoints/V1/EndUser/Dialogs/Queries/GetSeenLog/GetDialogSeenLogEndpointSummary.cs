using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.GetSeenLog;

public sealed class GetDialogSeenLogEndpointSummary : Summary<GetDialogSeenLogEndpoint>
{
    public GetDialogSeenLogEndpointSummary()
    {
        Summary = "Gets a single dialog seen log record";
        Description = """
                      Gets a single dialog seen log record.
                      """;

        Responses[Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("seen log record");
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<GetDialogSeenLogEndpoint>().Build();
    }
}
