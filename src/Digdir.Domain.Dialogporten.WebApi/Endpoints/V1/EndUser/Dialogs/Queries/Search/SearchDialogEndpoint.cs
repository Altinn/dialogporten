using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.Search;

[OpenApiOperationId("SearchDialogs")]
public sealed class SearchDialogEndpoint : Endpoint<SearchDialogRequest, PaginatedList<DialogDto>>
{
    private readonly ISender _sender;

    public SearchDialogEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    public override void Configure()
    {
        Get("dialogs");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        Description(b => b.ProducesOneOf<PaginatedList<DialogDto>>(
            StatusCodes.Status200OK,
            StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(SearchDialogRequest req, CancellationToken ct)
    {
        var query = req.ToSearchDialogQuery();
        var result = await _sender.Send(query, ct);

        await result.Match(
            paginatedDto => Send.OkAsync(paginatedDto, ct),
            validationError => this.BadRequestAsync(validationError, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            domainError => this.UnprocessableEntityAsync(domainError, ct));
    }
}

[OpenApiTypeName("SearchDialogsRequest")]
public sealed class SearchDialogRequest : SortablePaginationParameter<SearchDialogQueryOrderDefinition, DialogEntity>
{
    /// <summary>
    /// Filter by one or more service owner codes
    /// </summary>
    public List<string>? Org { get; init; }

    /// <summary>
    /// Filter by one or more service resources
    /// </summary>
    public List<string>? ServiceResource { get; set; }

    /// <summary>
    /// Filter by one or more owning parties
    /// </summary>
    public List<string>? Party { get; set; }

    /// <summary>
    /// Filter by one or more extended statuses
    /// </summary>
    public List<string>? ExtendedStatus { get; init; }

    /// <summary>
    /// Filter by external reference
    /// </summary>
    public string? ExternalReference { get; init; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public List<DialogStatus.Values>? Status { get; init; }

    /// <summary>
    /// Only return dialogs created after this date. For free text search this does not limit how much the search has to scan; use 'contentUpdatedAfter' to narrow a broad search and avoid a 422 timeout.
    /// </summary>
    public DateTimeOffset? CreatedAfter { get; set; }

    /// <summary>
    /// Only return dialogs created before this date. For free text search this does not limit how much the search has to scan; use 'contentUpdatedAfter' to narrow a broad search and avoid a 422 timeout.
    /// </summary>
    public DateTimeOffset? CreatedBefore { get; set; }

    /// <summary>
    /// Only return dialogs updated after this date. For free text search this does not limit how much the search has to scan; use 'contentUpdatedAfter' to narrow a broad search and avoid a 422 timeout.
    /// </summary>
    public DateTimeOffset? UpdatedAfter { get; set; }

    /// <summary>
    /// Only return dialogs updated before this date. For free text search this does not limit how much the search has to scan; use 'contentUpdatedAfter' to narrow a broad search and avoid a 422 timeout.
    /// </summary>
    public DateTimeOffset? UpdatedBefore { get; set; }

    /// <summary>
    /// Only return dialogs with content updated after this date. Recommended for free text search: this is the only date filter that limits how much a broad search has to scan. A broad search term without a 'contentUpdatedAfter' bound may exceed the server-side time limit and return 422 - narrow it with this filter (and/or fewer parties or a service resource).
    /// </summary>
    public DateTimeOffset? ContentUpdatedAfter { get; set; }

    /// <summary>
    /// Only return dialogs with content updated before this date. Unlike 'contentUpdatedAfter', this upper bound does not by itself limit how much a free text search has to scan.
    /// </summary>
    public DateTimeOffset? ContentUpdatedBefore { get; set; }

    /// <summary>
    /// Only return dialogs that have content that has/hasn't been seen.
    /// If null, no filtering is applied
    /// If true, returns dialogs that have been seen
    /// If false, returns dialogs that have not been seen
    ///
    /// A dialog's content is considered seen if:
    /// - It has been visited by the GET .../dialogs/{dialogId} endpoint since the last content update, and
    /// - It does not have a system label MarkedAsUnopened.
    /// </summary>
    public bool? IsContentSeen { get; set; }

    /// <summary>
    /// Only return dialogs with due date after this date
    /// </summary>
    public DateTimeOffset? DueAfter { get; set; }

    /// <summary>
    /// Only return dialogs with due date before this date
    /// </summary>
    public DateTimeOffset? DueBefore { get; set; }

    /// <summary>
    /// Filter by process
    /// </summary>
    public string? Process { get; init; }

    /// <summary>
    /// Filter by Display state
    /// </summary>
    public List<SystemLabel.Values>? SystemLabel { get; set; }

    /// <summary>
    /// Whether to exclude API-only dialogs from the results. Defaults to false.
    /// </summary>
    public bool? ExcludeApiOnly { get; init; }

    /// <summary>
    /// Search string for free text search. Will attempt to fuzzily match in all free text fields in the aggregate
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Limit free text search to texts with this language code, e.g. 'nb', 'en'. Culture codes will be normalized to neutral language codes (ISO 639). Default: search all culture codes
    /// </summary>
    public string? SearchLanguageCode
    {
        get;
        init => field = Localization.NormalizeCultureCode(value);
    }

    [FromHeader(Constants.AcceptLanguage, isRequired: false)]
    public AcceptedLanguages? AcceptedLanguages { get; set; } = null;
}
