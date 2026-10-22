using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchRelations;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.SearchRelations;

[OpenApiOperationId("SearchDialogRelations")]
[OpenApiExtras(
    scopes: [AuthorizationScope.EndUser],
    securitySchemes: [OpenApiSecurityScheme.IdportenSecurityScheme, OpenApiSecurityScheme.MaskinportenSecurityScheme])
]
public sealed class SearchDialogRelationsEndpoint : Endpoint<SearchDialogRelationsRequest, PaginatedList<DialogRelationDto>>
{
    private readonly ISender _sender;

    public SearchDialogRelationsEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    public override void Configure()
    {
        Get("dialogs/{dialogId}/relations");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        Description(b => b
            .Produces<PaginatedList<DialogRelationDto>>()
            .ProducesDpProblemFor(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound
            ));
    }

    public override async Task HandleAsync(SearchDialogRelationsRequest req, CancellationToken ct)
    {
        var query = new SearchDialogRelationsQuery
        {
            DialogId = req.DialogId,
            Type = req.Type,
            Party = req.Party,
            ServiceResource = req.ServiceResource,
            Status = req.Status,
            ExcludeApiOnly = req.ExcludeApiOnly,
            ContinuationToken = req.ContinuationToken,
            Limit = req.Limit,
            AcceptedLanguages = req.AcceptedLanguages?.AcceptedLanguage
        };

        var result = await _sender.Send(query, ct);
        await result.Match(
            page => Send.OkAsync(page, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            validationError => this.BadRequestAsync(validationError, ct));
    }
}

[OpenApiTypeName("SearchDialogRelationsRequest")]
public sealed class SearchDialogRelationsRequest
{
    public Guid DialogId { get; set; }

    /// <summary>
    /// Only return relations of this type, as seen from the dialog.
    /// </summary>
    public DialogRelationType.Values? Type { get; init; }

    /// <summary>
    /// Filter by one or more parties owning the related dialogs.
    /// </summary>
    public List<string>? Party { get; init; }

    /// <summary>
    /// Filter by one or more service resources of the related dialogs.
    /// </summary>
    public List<string>? ServiceResource { get; init; }

    /// <summary>
    /// Filter by one or more statuses of the related dialogs.
    /// </summary>
    public List<DialogStatus.Values>? Status { get; init; }

    /// <summary>
    /// Whether to exclude API-only dialogs from the results. Defaults to false.
    /// </summary>
    public bool? ExcludeApiOnly { get; init; }

    /// <summary>
    /// Continuation token for pagination.
    /// </summary>
    public string? ContinuationToken { get; init; }

    /// <summary>
    /// Limit the number of results returned. Defaults to 100, maximum 1000.
    /// </summary>
    public int? Limit { get; init; }

    [FromHeader(Constants.AcceptLanguage, isRequired: false)]
    public AcceptedLanguages? AcceptedLanguages { get; set; } = null;
}
