using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.DialogSeenLogs.Get;

public sealed class GetDialogSeenLogEndpointSummary : Summary<GetDialogSeenLogEndpoint>
{
    public GetDialogSeenLogEndpointSummary()
    {
        Summary = "Gets a single dialog seen log record";
        Description = """
                      Gets a single dialog seen log record.
                      """;

        Responses[StatusCodes.Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("seen log record");
        Responses[StatusCodes.Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[StatusCodes.Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
    }
}
