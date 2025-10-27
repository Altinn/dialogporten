using AutoMapper;
using AutoMapper.QueryableExtensions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;

public sealed class SearchDialogQuery : SortablePaginationParameter<SearchDialogQueryOrderDefinition, DialogEntity>, IRequest<SearchDialogResult>, IFeatureMetricServiceResourceIgnoreRequest
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
    public string? Search { get; set; }

    /// <summary>
    /// Limit free text search to texts with this language code, e.g. 'nb', 'en'. Culture codes will be normalized to neutral language codes (ISO 639). Default: search all culture codes
    /// </summary>
    public string? SearchLanguageCode
    {
        get => _searchLanguageCode;
        init => _searchLanguageCode = Localization.NormalizeCultureCode(value);
    }

    /// <summary>
    /// Accepted languages for localization filtering, sorted by preference
    /// </summary>
    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }
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
    private readonly IDialogSearchRepository _searchRepository;

    public SearchDialogQueryHandler(
        IDialogDbContext db,
        IMapper mapper,
        IClock clock,
        IUserRegistry userRegistry,
        IAltinnAuthorization altinnAuthorization,
        IDialogSearchRepository searchRepository)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
        _searchRepository = searchRepository ?? throw new ArgumentNullException(nameof(searchRepository));
    }

    public async Task<SearchDialogResult> Handle(SearchDialogQuery request, CancellationToken cancellationToken)
    {
        var authorizedResources = await _altinnAuthorization.GetAuthorizedResourcesForSearch(
            request.Party ?? [],
            request.ServiceResource ?? [],
            cancellationToken: cancellationToken);

        if (authorizedResources.HasNoAuthorizations)
        {
            return PaginatedList<DialogDto>.CreateEmpty(request);
        }

        var dialogs = await _searchRepository.GetDialogs(
            request.ToGetDialogsQuery(_clock.UtcNowOffset),
            authorizedResources,
            cancellationToken);

        var dialogIds = dialogs.Items.Select(x => x.Id).ToArray();
        var guiAttachmentCountByDialogId = await _db.DialogAttachments.AsNoTracking()
            .Where(x => dialogIds.Contains(x.DialogId))
            .Where(x => x.Urls.Any(url => url.ConsumerTypeId == AttachmentUrlConsumerType.Values.Gui))
            .GroupBy(x => x.DialogId)
            .ToDictionaryAsync(x => x.Key, x => x.Count(), cancellationToken);
        var contentByDialogId = await _db.DialogContents.AsNoTracking()
            .Where(x => dialogIds.Contains(x.DialogId))
            .Where(c => c.Type.OutputInList)
            .Include(x => x.Value.Localizations)
            .GroupBy(x => x.DialogId)
            .ToDictionaryAsync(x => x.Key, x => x.ToList(), cancellationToken);
        var systemLabelsByDialogId = await _db.DialogEndUserContexts.AsNoTracking()
            .Where(x => dialogIds.Contains(x.DialogId!.Value))
            .GroupBy(x => x.DialogId)
            .ToDictionaryAsync(x => x.Key!.Value, x => x
                .Select(g => new DialogEndUserContextDto
                {
                    Revision = g.Revision,
                    SystemLabels = g.DialogEndUserContextSystemLabels
                        .Select(l => l.SystemLabelId)
                        .ToList()
                })
                .First(), cancellationToken);
        var seenLogsByDialogId = await _db.DialogSeenLog.AsNoTracking()
            .Where(x => dialogIds.Contains(x.DialogId))
            // Do not need to filter on dialog.UpdatedAt because dialog.ContentUpdatedAt is always
            // before or equal to dialog.UpdatedAt
            .Where(l => l.CreatedAt >= l.Dialog.ContentUpdatedAt)
            .Include(x => x.SeenBy.ActorNameEntity)
            .GroupBy(x => x.DialogId)
            .ToDictionaryAsync(x => x.Key, x => x
                .Select(g => new DialogSeenLogDto
                {
                    SeenAt = g.CreatedAt,
                    Id = g.Id,
                    IsViaServiceOwner = g.IsViaServiceOwner,
                    SeenBy = new ActorDto
                    {
                        ActorId = g.SeenBy.ActorNameEntity!.ActorId,
                        ActorName = g.SeenBy.ActorNameEntity!.Name,
                        ActorType = g.SeenBy.ActorTypeId
                    }
                })
                .ToList(), cancellationToken);

        var currentUserId = _userRegistry.GetCurrentUserId().ExternalIdWithPrefix;
        foreach (var item in seenLogsByDialogId.Values.SelectMany(x => x))
        {
            item.IsCurrentEndUser = item.SeenBy.ActorId == currentUserId;
            item.SeenBy.ActorId = IdentifierMasker.GetMaybeMaskedIdentifier(item.SeenBy.ActorId);
        }

        var latestActivitiesByDialogId = await _db.DialogActivities.AsNoTracking()
            .Where(x => dialogIds.Contains(x.DialogId))
            .GroupBy(x => x.DialogId)
            .ToDictionaryAsync(x => x.Key, x => x
                .OrderByDescending(a => a.CreatedAt)
                .ThenByDescending(a => a.Id)
                .Select(a => new DialogActivityDto
                {
                    Id = a.Id,
                    CreatedAt = a.CreatedAt,
                    Type = a.TypeId,
                    ExtendedType = a.ExtendedType,
                    PerformedBy = new ActorDto
                    {
                        ActorId = a.PerformedBy.ActorNameEntity!.ActorId,
                        ActorName = a.PerformedBy.ActorNameEntity!.Name,
                        ActorType = a.PerformedBy.ActorTypeId
                    },
                    TransmissionId = a.TransmissionId,
                    Description = a.Description!.Localizations
                        .Select(l => new LocalizationDto
                        {
                            LanguageCode = l.LanguageCode,
                            Value = l.Value
                        })
                        .ToList()
                })
                .FirstOrDefault(), cancellationToken);

        foreach (var item in latestActivitiesByDialogId.Values
                     .Where(x => x is not null)
                     .Select(x => x!.PerformedBy))
        {
            item.ActorId = IdentifierMasker.GetMaybeMaskedIdentifier(item.ActorId);
        }

        // var paginatedList = await _db.Dialogs
        //     .PrefilterAuthorizedDialogs(authorizedResources)
        //     .AsNoTracking()
        //     .Include(x => x.Content)
        //         .ThenInclude(x => x.Value.Localizations)
        //     .WhereIf(!request.Org.IsNullOrEmpty(), x => request.Org!.Contains(x.Org))
        //     .WhereIf(!request.ServiceResource.IsNullOrEmpty(), x => request.ServiceResource!.Contains(x.ServiceResource))
        //     .WhereIf(!request.Party.IsNullOrEmpty(), x => request.Party!.Contains(x.Party))
        //     .WhereIf(!request.ExtendedStatus.IsNullOrEmpty(), x => x.ExtendedStatus != null && request.ExtendedStatus!.Contains(x.ExtendedStatus))
        //     .WhereIf(!string.IsNullOrWhiteSpace(request.ExternalReference),
        //         x => x.ExternalReference != null && request.ExternalReference == x.ExternalReference)
        //     .WhereIf(!request.Status.IsNullOrEmpty(), x => request.Status!.Contains(x.StatusId))
        //     .WhereIf(request.CreatedAfter.HasValue, x => request.CreatedAfter <= x.CreatedAt)
        //     .WhereIf(request.CreatedBefore.HasValue, x => x.CreatedAt <= request.CreatedBefore)
        //     .WhereIf(request.UpdatedAfter.HasValue, x => request.UpdatedAfter <= x.UpdatedAt)
        //     .WhereIf(request.UpdatedBefore.HasValue, x => x.UpdatedAt <= request.UpdatedBefore)
        //     .WhereIf(request.ContentUpdatedAfter.HasValue, x => request.ContentUpdatedAfter <= x.ContentUpdatedAt)
        //     .WhereIf(request.ContentUpdatedBefore.HasValue, x => x.ContentUpdatedAt <= request.ContentUpdatedBefore)
        //     .WhereIf(request.DueAfter.HasValue, x => request.DueAfter <= x.DueAt)
        //     .WhereIf(request.DueBefore.HasValue, x => x.DueAt <= request.DueBefore)
        //     .WhereIf(request.Process is not null, x => EF.Functions.ILike(x.Process!, request.Process!))
        //     .WhereIf(!request.SystemLabel.IsNullOrEmpty(), x =>
        //         request.SystemLabel!.All(label =>
        //             x.EndUserContext.DialogEndUserContextSystemLabels
        //                 .Any(sl => sl.SystemLabelId == label)))
        //     .WhereIf(request.Search is not null, x =>
        //         x.Content.Any(x => x.Value.Localizations.AsQueryable().Any(searchExpression)) ||
        //         x.SearchTags.Any(x => EF.Functions.ILike(x.Value, request.Search!))
        //     )
        //     .WhereIf(request.ExcludeApiOnly == true, x => !x.IsApiOnly)
        //     .Where(x => !x.VisibleFrom.HasValue || _clock.UtcNowOffset > x.VisibleFrom)
        //     .Where(x => !x.ExpiresAt.HasValue || x.ExpiresAt > _clock.UtcNowOffset)
        //     .ProjectTo<IntermediateDialogDto>(_mapper.ConfigurationProvider)
        //     .ToPaginatedListAsync(request, cancellationToken: cancellationToken);

        dialogs.Items.ForEach(x => x.FilterLocalizations(request.AcceptedLanguages));

        foreach (var dialog in dialogs.Items)
        {
            // This filtering cannot be done in AutoMapper using ProjectTo
            dialog.SeenSinceLastContentUpdate = dialog.SeenSinceLastContentUpdate
                .GroupBy(log => log.SeenBy.ActorId)
                .Select(group => group
                    .OrderByDescending(log => log.SeenAt)
                    .First())
                .ToList();
        }

        var serviceResources = dialogs.Items
            .Select(x => x.ServiceResource)
            .Distinct()
            .ToList();

        var resourcePolicyInformation = await _db.ResourcePolicyInformation
            .Where(x => serviceResources.Contains(x.Resource))
            .ToDictionaryAsync(x => x.Resource, x => x.MinimumAuthenticationLevel, cancellationToken);

        foreach (var dialog in dialogs.Items)
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

        return dialogs.ConvertTo(_mapper.Map<DialogDto>);
    }
}

internal static class SearchDialogQueryExtensions
{
    public static GetDialogsQuery ToGetDialogsQuery(this SearchDialogQuery request, DateTimeOffset nowUtc)
    {
        return new GetDialogsQuery
        {
            VisibleAfter = nowUtc,
            ExpiresBefore = nowUtc,
            Deleted = false,
            OrderBy = request.OrderBy,
            ContinuationToken = request.ContinuationToken,
            Limit = request.Limit!.Value,
            ContentUpdatedAfter = request.ContentUpdatedAfter,
            ContentUpdatedBefore = request.ContentUpdatedBefore,
            AcceptedLanguages = request.AcceptedLanguages,
            Search = request.Search,
            SearchLanguageCode = request.SearchLanguageCode,
            CreatedAfter = request.CreatedAfter,
            CreatedBefore = request.CreatedBefore,
            DueAfter = request.DueAfter,
            DueBefore = request.DueBefore,
            ExcludeApiOnly = request.ExcludeApiOnly,
            Process = request.Process,
            SystemLabel = request.SystemLabel,
            UpdatedAfter = request.UpdatedAfter,
            UpdatedBefore = request.UpdatedBefore,
            ExternalReference = request.ExternalReference,
            ExtendedStatus = request.ExtendedStatus,
            Org = request.Org,
            Party = request.Party,
            ServiceResource = request.ServiceResource,
            Status = request.Status,
        };
    }
}
