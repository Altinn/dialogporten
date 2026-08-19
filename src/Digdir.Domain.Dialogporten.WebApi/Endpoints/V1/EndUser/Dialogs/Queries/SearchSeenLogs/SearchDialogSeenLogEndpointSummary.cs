using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;

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

        Responses[Status200OK] = Constants.SwaggerSummary.ReturnedResult.FormatInvariant("seen log records");
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<SearchDialogSeenLogEndpoint>().Build();
    }
}
