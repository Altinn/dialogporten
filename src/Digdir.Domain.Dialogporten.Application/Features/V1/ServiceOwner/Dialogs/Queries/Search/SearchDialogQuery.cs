using System.Globalization;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;

public sealed class SearchDialogQuery : SortablePaginationParameter<SearchDialogQueryOrderDefinition, IntermediateDialogDto>, IRequest<SearchDialogResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    private string? _searchLanguageCode;

    /// <summary>
    /// Filter by one or more service resources
    /// </summary>
    public List<string>? ServiceResource { get; set; }

    /// <summary>
    /// Filter by one or more owning parties
    /// </summary>
    public List<string>? Party { get; set; }

    /// <summary>
    /// Filter by end user id
    /// </summary>
    public string? EndUserId { get; set; }

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

    private DeletedFilter? _deleted = DeletedFilter.Exclude;

    /// <summary>
    /// If set to 'include', the result will include both deleted and non-deleted dialogs. If set to 'exclude', the result will only include non-deleted dialogs. If set to 'only', the result will only include deleted dialogs
    /// </summary>
    public DeletedFilter? Deleted
    {
        get => _deleted;
        set => _deleted = value ?? DeletedFilter.Exclude;
    }

    /// <summary>
    /// Only return dialogs created after this date
    /// </summary>
    public DateTimeOffset? CreatedAfter { get; set; }

    /// <summary>
    /// Only return dialogs created before this date
    /// </summary>
    public DateTimeOffset? CreatedBefore { get; set; }

    /// <summary>
    /// Only return dialogs updated after this date
    /// </summary>
    public DateTimeOffset? UpdatedAfter { get; set; }

    /// <summary>
    /// Only return dialogs updated before this date
    /// </summary>
    public DateTimeOffset? UpdatedBefore { get; set; }

    /// <summary>
    /// Only return dialogs with content updated after this date
    /// </summary>
    public DateTimeOffset? ContentUpdatedAfter { get; set; }

    /// <summary>
    /// Only return dialogs with content updated before this date
    /// </summary>
    public DateTimeOffset? ContentUpdatedBefore { get; set; }

    /// <summary>
    /// Only return dialogs with due date after this date
    /// </summary>
    public DateTimeOffset? DueAfter { get; set; }

    /// <summary>
    /// Only return dialogs with due date before this date
    /// </summary>
    public DateTimeOffset? DueBefore { get; set; }

    /// <summary>
    /// Only return dialogs with visible-from date after this date
    /// </summary>
    public DateTimeOffset? VisibleAfter { get; set; }

    /// <summary>
    /// Only return dialogs with visible-from date before this date
    /// </summary>
    public DateTimeOffset? VisibleBefore { get; set; }

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
    public string? Search { get; set; }

    /// <summary>
    /// Filter by one or more labels. Multiple labels are combined with AND, i.e., all labels must match. Supports prefix matching with '*' at the end of the label. For example, 'label*' will match 'label', 'label1', 'label2', etc.
    /// </summary>
    public List<string>? ServiceOwnerLabels { get; set; }

    /// <summary>
    /// Limit free text search to texts with this language code, e.g. 'nb', 'en'. Culture codes will be normalized to neutral language codes (ISO 639). Default: search all culture codes
    /// </summary>
    public string? SearchLanguageCode
    {
        get => _searchLanguageCode;
        init => _searchLanguageCode = Localization.NormalizeCultureCode(value);
    }
}

public sealed class SearchDialogQueryOrderDefinition : IOrderDefinition<IntermediateDialogDto>
{
    public static IOrderOptions<IntermediateDialogDto> Configure(IOrderOptionsBuilder<IntermediateDialogDto> options) =>
        options.AddId(x => x.Id)
            .AddDefault("createdAt", x => x.CreatedAt)
            .AddOption("updatedAt", x => x.UpdatedAt)
            .AddOption("contentUpdatedAt", x => x.ContentUpdatedAt)
            .AddOption("dueAt", x => x.DueAt)
            .Build();
}

[GenerateOneOf]
public sealed partial class SearchDialogResult : OneOfBase<PaginatedList<DialogDto>, ValidationError>;

internal sealed class SearchDialogQueryHandler : IRequestHandler<SearchDialogQuery, SearchDialogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public SearchDialogQueryHandler(
        IDialogDbContext db,
        IMapper mapper,
        IUserResourceRegistry userResourceRegistry,
        IAltinnAuthorization altinnAuthorization)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
        _altinnAuthorization = altinnAuthorization;
    }

    public async Task<SearchDialogResult> Handle(SearchDialogQuery request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);
        var searchExpression = Expressions.LocalizedSearchExpression(request.Search, request.SearchLanguageCode);

        var dialogQuery = _db.Dialogs.AsQueryable();

        // If the service owner impersonates an end user, we need to filter the dialogs
        // based on the end user's authorization, not the service owner's (which is
        // allowed to see everything about every service resource they own).
        if (request.EndUserId is not null)
        {
            var authorizedResources = await _altinnAuthorization.GetAuthorizedResourcesForSearch(
                request.Party ?? [],
                request.ServiceResource ?? [],
                cancellationToken);
            dialogQuery = _db.Dialogs.PrefilterAuthorizedDialogs(authorizedResources);
        }

        var formattedServiceOwnerLabels = request.ServiceOwnerLabels?
            .Select(label => label.EndsWith("*", StringComparison.OrdinalIgnoreCase)
                ? label.TrimEnd('*').ToLower(CultureInfo.InvariantCulture) + "%"
                : label.ToLower(CultureInfo.InvariantCulture))
            .ToList();

        var paginatedList = await _db.WrapWithRepeatableRead((dbCtx, ct) =>
            dialogQuery
                .Include(x => x.Content)
                    .ThenInclude(x => x.Value.Localizations)
                .Include(x => x.ServiceOwnerContext)
                    .ThenInclude(x => x.ServiceOwnerLabels)
                .WhereIf(!request.ServiceResource.IsNullOrEmpty(),
                    x => request.ServiceResource!.Contains(x.ServiceResource))
                .WhereIf(!request.Party.IsNullOrEmpty(), x => request.Party!.Contains(x.Party))
                .WhereIf(!request.ExtendedStatus.IsNullOrEmpty(),
                    x => x.ExtendedStatus != null && request.ExtendedStatus!.Contains(x.ExtendedStatus))
                .WhereIf(!string.IsNullOrWhiteSpace(request.ExternalReference),
                    x => x.ExternalReference != null && request.ExternalReference == x.ExternalReference)
                .WhereIf(!request.Status.IsNullOrEmpty(), x => request.Status!.Contains(x.StatusId))
                .WhereIf(request.CreatedAfter.HasValue, x => request.CreatedAfter <= x.CreatedAt)
                .WhereIf(request.CreatedBefore.HasValue, x => x.CreatedAt <= request.CreatedBefore)
                .WhereIf(request.UpdatedAfter.HasValue, x => request.UpdatedAfter <= x.UpdatedAt)
                .WhereIf(request.UpdatedBefore.HasValue, x => x.UpdatedAt <= request.UpdatedBefore)
                .WhereIf(request.ContentUpdatedAfter.HasValue, x => request.ContentUpdatedAfter <= x.ContentUpdatedAt)
                .WhereIf(request.ContentUpdatedBefore.HasValue, x => x.ContentUpdatedAt <= request.ContentUpdatedBefore)
                .WhereIf(request.DueAfter.HasValue, x => request.DueAfter <= x.DueAt)
                .WhereIf(request.DueBefore.HasValue, x => x.DueAt <= request.DueBefore)
                .WhereIf(request.Process is not null, x => EF.Functions.ILike(x.Process!, request.Process!))
                .WhereIf(request.VisibleAfter.HasValue, x => request.VisibleAfter <= x.VisibleFrom)
                .WhereIf(request.VisibleBefore.HasValue, x => x.VisibleFrom <= request.VisibleBefore)
                .WhereIf(!request.SystemLabel.IsNullOrEmpty(), x =>
                    request.SystemLabel!.All(label =>
                        x.EndUserContext.DialogEndUserContextSystemLabels
                            .Any(sl => sl.SystemLabelId == label)))
                .WhereIf(request.Search is not null, x =>
                    x.Content.Any(x => x.Value.Localizations.AsQueryable().Any(searchExpression)) ||
                    x.SearchTags.Any(x => EF.Functions.ILike(x.Value, request.Search!))
                )
                .WhereIf(request.Deleted == DeletedFilter.Exclude, x => !x.Deleted)
                .WhereIf(request.Deleted == DeletedFilter.Only, x => x.Deleted)
                .WhereIf(formattedServiceOwnerLabels is not null && formattedServiceOwnerLabels.Count != 0, x =>
                    formattedServiceOwnerLabels!
                        .All(formattedLabel =>
                            x.ServiceOwnerContext.ServiceOwnerLabels
                                .Any(l => EF.Functions.Like(l.Value, formattedLabel))))
                .WhereIf(request.ExcludeApiOnly == true, x => !x.IsApiOnly)
                .Where(x => resourceIds.Contains(x.ServiceResource))
                .IgnoreQueryFilters()
                .ProjectTo<IntermediateDialogDto>(_mapper.ConfigurationProvider)
                .ToPaginatedListAsync(request, cancellationToken: ct),
            cancellationToken);

        foreach (var dialog in paginatedList.Items)
        {
            // This filtering cannot be done in AutoMapper using ProjectTo
            dialog.SeenSinceLastContentUpdate = dialog.SeenSinceLastContentUpdate
                .GroupBy(log => log.SeenBy.ActorId)
                .Select(group => group
                    .OrderByDescending(log => log.SeenAt)
                    .First())
                .ToList();
        }

        if (request.EndUserId is not null)
        {
            var seenRecords = paginatedList.Items
                .SelectMany(x => x.SeenSinceLastContentUpdate)
                .Concat(paginatedList.Items.SelectMany(x => x.SeenSinceLastUpdate))
                .ToList();

            foreach (var seenRecord in seenRecords)
            {
                seenRecord.IsCurrentEndUser = seenRecord.SeenBy.ActorId == request.EndUserId;
            }
        }

        return paginatedList.ConvertTo(_mapper.Map<DialogDto>);
    }
}
