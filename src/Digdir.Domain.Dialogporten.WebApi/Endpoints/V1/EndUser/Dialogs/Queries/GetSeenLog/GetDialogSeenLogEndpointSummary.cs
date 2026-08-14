using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
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
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<GetDialogSeenLogEndpoint>();
    }
}
