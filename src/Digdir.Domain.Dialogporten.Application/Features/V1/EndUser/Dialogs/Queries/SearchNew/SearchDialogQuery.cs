using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchNew;

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
    private readonly IClock _clock;
    private readonly IUserRegistry _userRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IDialogSearchRepository _searchRepository;
    private readonly IUser _user;

    public SearchDialogQueryHandler(
        IDialogDbContext db,
        IClock clock,
        IUserRegistry userRegistry,
        IAltinnAuthorization altinnAuthorization,
        IDialogSearchRepository searchRepository,
        IUser user)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
        _searchRepository = searchRepository ?? throw new ArgumentNullException(nameof(searchRepository));
        _user = user ?? throw new ArgumentNullException(nameof(user));
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

        var dialogIds = dialogs.Items
            .Select(x => x.Id)
            .ToArray();

        if (dialogIds.Length == 0)
        {
            return PaginatedList<DialogDto>.CreateEmpty(request);
        }

        var guiAttachmentCountByDialogId = await FetchGuiAttachmentCountByDialogId(dialogIds, cancellationToken);
        var contentByDialogId = await FetchContentByDialogId(dialogIds, cancellationToken);
        var endUserContextByDialogId = await FetchEndUserContextByDialogId(dialogIds, cancellationToken);
        var seenLogsByDialogId = await FetchSeenLogByDialogId(dialogIds, cancellationToken);
        var latestActivitiesByDialogId = await FetchLatestActivitiesByDialogId(dialogIds, cancellationToken);

        MaskActorIdentifiers(seenLogsByDialogId, latestActivitiesByDialogId);

        var localizationSets = Enumerable.Empty<List<LocalizationDto>>()
            .Concat(contentByDialogId.Values.Select(x => x.ExtendedStatus?.Value))
            .Concat(contentByDialogId.Values.Select(x => x.SenderName?.Value))
            .Concat(contentByDialogId.Values.Select(x => x.Summary?.Value))
            .Concat(contentByDialogId.Values.Select(x => x.Title.Value))
            .Concat(latestActivitiesByDialogId.Values.Select(x => x?.Description));
        foreach (var localizationSet in localizationSets)
        {
            localizationSet.PruneLocalizations(request.AcceptedLanguages);
        }

        var result = dialogs.ConvertTo(dialog =>
        {
            return new DialogDto
            {
                Id = dialog.Id,
                Org = dialog.Org,
                ServiceResource = dialog.ServiceResource,
                ServiceResourceType = dialog.ServiceResourceType,
                Party = dialog.Party,
                Progress = dialog.Progress,
                Process = dialog.Process,
                PrecedingProcess = dialog.PrecedingProcess,
                GuiAttachmentCount = guiAttachmentCountByDialogId.GetValueOrDefault(dialog.Id),
                ExtendedStatus = dialog.ExtendedStatus,
                ExternalReference = dialog.ExternalReference,
                CreatedAt = dialog.CreatedAt,
                UpdatedAt = dialog.UpdatedAt,
                ContentUpdatedAt = dialog.ContentUpdatedAt,
                DueAt = dialog.DueAt,
                Status = dialog.StatusId,
                HasUnopenedContent = dialog.HasUnopenedContent,
                SystemLabel = endUserContextByDialogId[dialog.Id].SystemLabels
                    .FirstOrDefault(SystemLabel.DefaultArchiveBinGroup.Contains),
                IsApiOnly = dialog.IsApiOnly,
                FromServiceOwnerTransmissionsCount = dialog.FromServiceOwnerTransmissionsCount,
                FromPartyTransmissionsCount = dialog.FromPartyTransmissionsCount,
                LatestActivity = latestActivitiesByDialogId.GetValueOrDefault(dialog.Id),
                SeenSinceLastUpdate = seenLogsByDialogId
                    .GetValueOrDefault(dialog.Id)?
                    .Where(x => x.SeenAt >= dialog.UpdatedAt)
                    .GroupBy(x => x.SeenBy.ActorId)
                    .Select(g => g.OrderByDescending(x => x.SeenAt).First())
                    .ToList() ?? [],
                SeenSinceLastContentUpdate = seenLogsByDialogId
                    .GetValueOrDefault(dialog.Id)?
                    .Where(x => x.SeenAt >= dialog.ContentUpdatedAt)
                    .GroupBy(x => x.SeenBy.ActorId)
                    .Select(g => g.OrderByDescending(x => x.SeenAt).First())
                    .ToList() ?? [],
                EndUserContext = endUserContextByDialogId[dialog.Id],
                Content = contentByDialogId[dialog.Id],
            };
        });

        return result;
    }

    private static void MaskActorIdentifiers(Dictionary<Guid, List<DialogSeenLogDto>> seenLogsByDialogId, Dictionary<Guid, DialogActivityDto?> latestActivitiesByDialogId)
    {
        foreach (var item in seenLogsByDialogId.Values
                     .SelectMany(x => x)
                     .Select(x => x.SeenBy)
                     .Concat(latestActivitiesByDialogId.Values
                         .Where(x => x is not null)
                         .Select(x => x!.PerformedBy)))
        {
            item.ActorId = IdentifierMasker.GetMaybeMaskedIdentifier(item.ActorId);
        }
    }

    private async Task<Dictionary<Guid, DialogActivityDto?>> FetchLatestActivitiesByDialogId(Guid[] dialogIds,
        CancellationToken cancellationToken)
    {
        return await _db.DialogActivities.AsNoTracking()
            .Where(x => dialogIds.Contains(x.DialogId))
            .Select(a => new
            {
                a.DialogId,
                Activity = new DialogActivityDto
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
                }
            })
            .GroupBy(x => x.DialogId)
            .ToDictionaryAsync(x => x.Key, g => g
                .Select(x => x.Activity)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault(), cancellationToken);
    }

    private async Task<Dictionary<Guid, List<DialogSeenLogDto>>> FetchSeenLogByDialogId(Guid[] dialogIds,
        CancellationToken cancellationToken)
    {
        var currentUserId = _userRegistry.GetCurrentUserId().ExternalIdWithPrefix;
        var seenLogsByDialogId = await _db.DialogSeenLog.AsNoTracking()
            .Where(x => dialogIds.Contains(x.DialogId))
            // Do not need to filter on dialog.UpdatedAt because dialog.ContentUpdatedAt is always
            // before or equal to dialog.UpdatedAt
            .Where(l => l.CreatedAt >= l.Dialog.ContentUpdatedAt)
            .Select(x => new
            {
                x.DialogId,
                SeenLog = new DialogSeenLogDto
                {
                    Id = x.Id,
                    SeenAt = x.CreatedAt,
                    IsViaServiceOwner = x.IsViaServiceOwner,
                    IsCurrentEndUser = x.SeenBy.ActorNameEntity!.ActorId == currentUserId,
                    SeenBy = new ActorDto
                    {
                        ActorId = x.SeenBy.ActorNameEntity!.ActorId,
                        ActorName = x.SeenBy.ActorNameEntity!.Name,
                        ActorType = x.SeenBy.ActorTypeId
                    }
                }
            })
            .GroupBy(x => x.DialogId)
            .ToDictionaryAsync(x => x.Key, x => x
                .Select(g => g.SeenLog)
                .ToList(), cancellationToken);
        return seenLogsByDialogId;
    }

    private async Task<Dictionary<Guid, DialogEndUserContextDto>> FetchEndUserContextByDialogId(Guid[] dialogIds,
        CancellationToken cancellationToken)
    {
        return await _db.DialogEndUserContexts.AsNoTracking()
            .Where(x => dialogIds.Contains(x.DialogId!.Value))
            .Select(x => new
            {
                x.DialogId,
                EndUserContext = new DialogEndUserContextDto
                {
                    Revision = x.Revision,
                    SystemLabels = x.DialogEndUserContextSystemLabels
                        .Select(l => l.SystemLabelId)
                        .ToList()
                }
            })
            .GroupBy(x => x.DialogId)
            .ToDictionaryAsync(x => x.Key!.Value, x => x
                .Select(g => g.EndUserContext)
                .First(), cancellationToken);
    }

    private async Task<Dictionary<Guid, ContentDto>> FetchContentByDialogId(Guid[] dialogIds, CancellationToken cancellationToken)
    {
        var userAuthLevel = _user.GetPrincipal().GetAuthenticationLevel();
        var queryResult = await _db.DialogContents.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => dialogIds.Contains(x.DialogId))
            .Where(c => c.Type.OutputInList)
            .GroupJoin(_db.ResourcePolicyInformation,
                x => x.Dialog.ServiceResource,
                x => x.Resource,
                (x, policy) => new
                {
                    x.DialogId,
                    Type = x.TypeId,
                    Content = new ContentValueDto
                    {
                        MediaType = x.MediaType,
                        Value = x.Value.Localizations
                            .Select(l => new LocalizationDto { LanguageCode = l.LanguageCode, Value = l.Value })
                            .ToList()
                    },
                    HasRequiredAuthLevel = policy
                        .Select(p => p.MinimumAuthenticationLevel)
                        .FirstOrDefault() <= userAuthLevel
                })
            .GroupBy(x => x.DialogId)
            .ToDictionaryAsync(x => x.Key, x => x.ToList(), cancellationToken);
        return queryResult.ToDictionary(x => x.Key, group => new ContentDto
        {
            Title = group.Value
                .Where(x => x.Type is DialogContentType.Values.Title or DialogContentType.Values.NonSensitiveTitle)
                .OrderBy(x => x switch
                {
                    { HasRequiredAuthLevel: true, Type: DialogContentType.Values.Title } => 0,
                    { HasRequiredAuthLevel: false, Type: DialogContentType.Values.NonSensitiveTitle } => 1,
                    { Type: DialogContentType.Values.Title } => 2,
                    _ => 3
                })
                .Select(x => x.Content)
                .First(),
            Summary = group.Value
                .Where(x => x.Type is DialogContentType.Values.Summary or DialogContentType.Values.NonSensitiveSummary)
                .OrderBy(x => x switch
                {
                    { HasRequiredAuthLevel: true, Type: DialogContentType.Values.Summary } => 0,
                    { HasRequiredAuthLevel: false, Type: DialogContentType.Values.NonSensitiveSummary } => 1,
                    { Type: DialogContentType.Values.Summary } => 2,
                    _ => 3
                })
                .Select(x => x.Content)
                .FirstOrDefault(),
            ExtendedStatus = group.Value.FirstOrDefault(x => x.Type == DialogContentType.Values.ExtendedStatus)?.Content,
            SenderName = group.Value.FirstOrDefault(x => x.Type == DialogContentType.Values.SenderName)?.Content,
        });
    }

    private async Task<Dictionary<Guid, int>> FetchGuiAttachmentCountByDialogId(Guid[] dialogIds,
        CancellationToken cancellationToken)
    {
        return await _db.DialogAttachments.AsNoTracking()
            .Where(x => dialogIds.Contains(x.DialogId))
            .Where(x => x.Urls.Any(url => url.ConsumerTypeId == AttachmentUrlConsumerType.Values.Gui))
            .GroupBy(x => x.DialogId)
            .ToDictionaryAsync(x => x.Key, x => x.Count(), cancellationToken);
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
            OrderBy = request.OrderBy.DefaultIfNull(),
            ContinuationToken = request.ContinuationToken,
            Limit = request.Limit!.Value,
            ContentUpdatedAfter = request.ContentUpdatedAfter,
            ContentUpdatedBefore = request.ContentUpdatedBefore,
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
