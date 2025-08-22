using AutoMapper;
using AutoMapper.QueryableExtensions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Context;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;

public sealed class SearchDialogQuery : SortablePaginationParameter<SearchDialogQueryOrderDefinition, IntermediateDialogDto>, IRequest<SearchDialogResult>
{
    private readonly string? _searchLanguageCode;

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
public sealed partial class SearchDialogResult : OneOfBase<PaginatedList<DialogDto>, ValidationError, Forbidden>;

internal sealed class SearchDialogQueryHandler : IRequestHandler<SearchDialogQuery, SearchDialogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;
    private readonly IClock _clock;
    private readonly IUserRegistry _userRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IApplicationContext _applicationContext;

    public SearchDialogQueryHandler(
        IDialogDbContext db,
        IMapper mapper,
        IClock clock,
        IUserRegistry userRegistry,
        IAltinnAuthorization altinnAuthorization,
        IApplicationContext applicationContext)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
        _applicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
    }

    public async Task<SearchDialogResult> Handle(SearchDialogQuery request, CancellationToken cancellationToken)
    {
        var searchExpression = Expressions.LocalizedSearchExpression(request.Search, request.SearchLanguageCode);
        var authorizedResources = await _altinnAuthorization.GetAuthorizedResourcesForSearch(
            request.Party ?? [],
            request.ServiceResource ?? [],
            cancellationToken: cancellationToken);

        if (authorizedResources.HasNoAuthorizations)
        {
            return PaginatedList<DialogDto>.CreateEmpty(request);
        }

        var paginatedList = await _db.Dialogs
            .PrefilterAuthorizedDialogs(authorizedResources)
            .AsNoTracking()
            .Include(x => x.Content)
                .ThenInclude(x => x.Value.Localizations)
            .WhereIf(!request.Org.IsNullOrEmpty(), x => request.Org!.Contains(x.Org))
            .WhereIf(!request.ServiceResource.IsNullOrEmpty(), x => request.ServiceResource!.Contains(x.ServiceResource))
            .WhereIf(!request.Party.IsNullOrEmpty(), x => request.Party!.Contains(x.Party))
            .WhereIf(!request.ExtendedStatus.IsNullOrEmpty(), x => x.ExtendedStatus != null && request.ExtendedStatus!.Contains(x.ExtendedStatus))
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
            .WhereIf(!request.SystemLabel.IsNullOrEmpty(), x =>
                request.SystemLabel!.All(label =>
                    x.EndUserContext.DialogEndUserContextSystemLabels
                        .Any(sl => sl.SystemLabelId == label)))
            .WhereIf(request.Search is not null, x =>
                x.Content.Any(x => x.Value.Localizations.AsQueryable().Any(searchExpression)) ||
                x.SearchTags.Any(x => EF.Functions.ILike(x.Value, request.Search!))
            )
            .WhereIf(request.ExcludeApiOnly == true, x => !x.IsApiOnly)
            .Where(x => !x.VisibleFrom.HasValue || _clock.UtcNowOffset > x.VisibleFrom)
            .Where(x => !x.ExpiresAt.HasValue || x.ExpiresAt > _clock.UtcNowOffset)
            .ProjectTo<IntermediateDialogDto>(_mapper.ConfigurationProvider)
            .ToPaginatedListAsync(request, cancellationToken: cancellationToken);

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

        var seenLogs = paginatedList.Items
            .SelectMany(x => x.SeenSinceLastContentUpdate)
            .Concat(paginatedList.Items.SelectMany(x => x.SeenSinceLastUpdate))
            .ToList();

        foreach (var seenLog in seenLogs)
        {
            seenLog.IsCurrentEndUser = IdentifierMasker
                .GetMaybeMaskedIdentifier(_userRegistry
                    .GetCurrentUserId()
                    .ExternalIdWithPrefix) == seenLog.SeenBy.ActorId;
        }

        var serviceResources = paginatedList.Items
            .Select(x => x.ServiceResource)
            .Distinct()
            .ToList();

        var resourcePolicyInformation = await _db.ResourcePolicyInformation
            .Where(x => serviceResources.Contains(x.Resource))
            .ToDictionaryAsync(x => x.Resource, x => x.MinimumAuthenticationLevel, cancellationToken);

        foreach (var dialog in paginatedList.Items)
        {
            if (!resourcePolicyInformation.TryGetValue(dialog.ServiceResource, out var minimumAuthenticationLevel))
            {
                continue;
            }

            if (!_altinnAuthorization.UserHasRequiredAuthLevel(minimumAuthenticationLevel))
            {
                dialog.Content.SetNonSensitiveContent();
            }
        }

        // Add metadata for cost management
        if (paginatedList.Items.Count > 0)
        {
            // Use first dialog as representative for metadata
            var firstDialog = paginatedList.Items.First();
            _applicationContext.AddMetadata("org", firstDialog.Org);
            _applicationContext.AddMetadata("serviceResource", firstDialog.ServiceResource);
        }
        else
        {
            // For search operations with no results, set placeholder values
            _applicationContext.AddMetadata("org", "search_operation");
            _applicationContext.AddMetadata("serviceResource", "search_operation");
        }

        return paginatedList.ConvertTo(_mapper.Map<DialogDto>);
    }
}
