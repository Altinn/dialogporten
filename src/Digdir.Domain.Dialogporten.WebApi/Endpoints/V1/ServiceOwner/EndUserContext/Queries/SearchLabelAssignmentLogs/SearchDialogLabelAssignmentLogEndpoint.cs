using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Queries.SearchLabelAssignmentLog;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.EndUserContext.Queries.SearchLabelAssignmentLogs;

[OpenApiOperationId("SearchDialogLabelAssignmentLogs")]
[OpenApiExtras(
    scopes: [AuthorizationScope.ServiceProvider],
    securitySchemes: [OpenApiSecurityScheme.MaskinportenSecurityScheme])
]
public sealed class SearchDialogLabelAssignmentLogEndpoint : Endpoint<SearchDialogLabelAssignmentLogRequest, List<LabelAssignmentLogDto>>
{
    private readonly ISender _sender;

    public SearchDialogLabelAssignmentLogEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    public override void Configure()
    {
        Get("dialogs/{dialogId}/endusercontext/labellog");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(d => d
            .Produces<List<LabelAssignmentLogDto>>()
            .ProducesDpProblemFor(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status410Gone
            ));
    }

    public override async Task HandleAsync(SearchDialogLabelAssignmentLogRequest req, CancellationToken ct)
    {
        var query = new SearchLabelAssignmentLogQuery
        {
            DialogId = req.DialogId,
            EndUserId = req.EnduserId
        };

        var result = await _sender.Send(query, ct);
        await result.Match(
            dto => Send.OkAsync(dto, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            deleted => this.GoneAsync(deleted, ct));
    }
}

[OpenApiTypeName(nameof(SearchDialogLabelAssignmentLogRequest))]
public sealed class SearchDialogLabelAssignmentLogRequest
{
    public Guid DialogId { get; set; }

    /// <summary>
    /// Optional end user to act on behalf of. If supplied, the end user must have access to the dialog.
    /// </summary>
    /// <example>urn:altinn:person:identifier-no:01125512345</example>
    [QueryParam]
    public string? EnduserId { get; init; }
}
