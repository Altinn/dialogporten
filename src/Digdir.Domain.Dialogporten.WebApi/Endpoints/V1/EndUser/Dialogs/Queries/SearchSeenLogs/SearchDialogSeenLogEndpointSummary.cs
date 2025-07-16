using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.SearchSeenLogs;

public sealed class SearchDialogSeenLogEndpointSummary : Summary<SearchDialogSeenLogEndpoint>
{
    public SearchDialogSeenLogEndpointSummary()
    {
        const string summary = "Gets all seen log records for a dialog";
        Summary = summary;
        Description = $"""
                      {summary}.
                      """;

        Responses[StatusCodes.Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("seen log records");
        Responses[StatusCodes.Status401Unauthorized] = Constants.SwaggerSummary.EndUserAuthenticationFailure;
    }
}
