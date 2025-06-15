using AutoMapper;
using AutoMapper.QueryableExtensions;
using Digdir.Domain.Dialogporten.Application.Common;
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
using Digdir.Domain.Dialogporten.Domain.Attachments;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;

public sealed class SearchDialogQuery : SortablePaginationParameter<SearchDialogQueryOrderDefinition, DialogEntity>, IRequest<SearchDialogResult>
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

public sealed class SearchDialogQueryOrderDefinition : IOrderDefinition<DialogEntity>
{
    public static IOrderOptions<DialogEntity> Configure(IOrderOptionsBuilder<DialogEntity> options) =>
        options.AddId(x => x.Id)
            .AddDefault("createdAt", x => x.CreatedAt)
            .AddOption("updatedAt", x => x.UpdatedAt)
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

    public SearchDialogQueryHandler(
        IDialogDbContext db,
        IMapper mapper,
        IClock clock,
        IUserRegistry userRegistry,
        IAltinnAuthorization altinnAuthorization)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
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

        var baseQuery = _db.Dialogs
            .PrefilterAuthorizedDialogs(authorizedResources)
            .AsNoTracking()
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
            .WhereIf(request.DueAfter.HasValue, x => request.DueAfter <= x.DueAt)
            .WhereIf(request.DueBefore.HasValue, x => x.DueAt <= request.DueBefore)
            .WhereIf(request.Process is not null, x => EF.Functions.ILike(x.Process!, request.Process!))
            .WhereIf(!request.SystemLabel.IsNullOrEmpty(), x => request.SystemLabel!.Contains(x.DialogEndUserContext.SystemLabelId))
            .WhereIf(request.Search is not null, x =>
                x.Content.Any(x => x.Value.Localizations.AsQueryable().Any(searchExpression)) ||
                x.SearchTags.Any(x => EF.Functions.ILike(x.Value, request.Search!))
            )
            .WhereIf(request.ExcludeApiOnly == true, x => !x.IsApiOnly)
            .Where(x => !x.VisibleFrom.HasValue || _clock.UtcNowOffset > x.VisibleFrom)
            .Where(x => !x.ExpiresAt.HasValue || x.ExpiresAt > _clock.UtcNowOffset)
            ;

        var paginatedEntities = await baseQuery
            .ToPaginatedListAsync<SearchDialogQueryOrderDefinition, DialogEntity>(request, cancellationToken);

        var ids = paginatedEntities.Items.Select(x => x.Id).ToList();

        var dialogs = await _db.Dialogs
            .AsNoTracking()
            .Include(x => x.Content)
                .ThenInclude(x => x.Value.Localizations)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var serviceResources = dialogs
            .Select(x => x.ServiceResource)
            .Distinct()
            .ToList();

        var resourcePolicyInformation = await _db.ResourcePolicyInformation
            .Where(x => serviceResources.Contains(x.Resource))
            .ToDictionaryAsync(x => x.Resource, x => x.MinimumAuthenticationLevel, cancellationToken);

        foreach (var dialog in dialogs)
        {
            if (resourcePolicyInformation.TryGetValue(dialog.ServiceResource, out var minimumAuthenticationLevel) &&
                !_altinnAuthorization.UserHasRequiredAuthLevel(minimumAuthenticationLevel))
            {
                dialog.Content.SetNonSensitiveContent();
            }
        }

        var mappedDialogs = dialogs
            .Select(d => _mapper.Map<DialogDto>(_mapper.Map<IntermediateDialogDto>(d)))
            .ToList();

        var latestActivities = await _db.DialogActivities
            .AsNoTracking()
            .Where(x => ids.Contains(x.DialogId))
            .Include(x => x.PerformedBy.ActorNameEntity)
            .Include(x => x.Description!)
                .ThenInclude(x => x.Localizations)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        var latestActivityById = latestActivities
            .GroupBy(x => x.DialogId)
            .ToDictionary(g => g.Key, g => g.First());

        var seenLogs = await _db.DialogSeenLog
            .AsNoTracking()
            .Where(x => ids.Contains(x.DialogId))
            .Include(x => x.SeenBy.ActorNameEntity)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var seenLogsById = seenLogs.GroupBy(x => x.DialogId).ToDictionary(g => g.Key, g => g.ToList());

        var attachmentCounts = await _db.Dialogs
            .Where(x => ids.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                Count = x.Attachments.Count(a => a.Urls.Any(u => u.ConsumerTypeId == AttachmentUrlConsumerType.Values.Gui))
            })
            .ToListAsync(cancellationToken);

        var attachmentCountById = attachmentCounts.ToDictionary(x => x.Id, x => x.Count);

        foreach (var dto in mappedDialogs)
        {
            if (latestActivityById.TryGetValue(dto.Id, out var activity))
            {
                dto.LatestActivity = _mapper.Map<DialogActivityDto>(activity);
            }

            if (seenLogsById.TryGetValue(dto.Id, out var logs))
            {
                dto.SeenSinceLastUpdate = logs
                    .Where(l => l.CreatedAt >= dto.UpdatedAt)
                    .Select(_mapper.Map<DialogSeenLogDto>)
                    .ToList();

                foreach (var seenLog in dto.SeenSinceLastUpdate)
                {
                    seenLog.IsCurrentEndUser = IdentifierMasker.GetMaybeMaskedIdentifier(_userRegistry.GetCurrentUserId().ExternalIdWithPrefix) == seenLog.SeenBy.ActorId;
                }
            }

            if (attachmentCountById.TryGetValue(dto.Id, out var count))
            {
                dto.GuiAttachmentCount = count;
            }
        }

        var orderIndex = ids.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
        var ordered = mappedDialogs.OrderBy(x => orderIndex[x.Id]).ToList();

        return new PaginatedList<DialogDto>(ordered, paginatedEntities.HasNextPage, paginatedEntities.ContinuationToken, paginatedEntities.OrderBy);
    }
}
