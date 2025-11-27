using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
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
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
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
    private readonly IClock _clock;
    private readonly IUserRegistry _userRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IDialogSearchRepository _searchRepository;
    private readonly IUser _user;

    public SearchDialogQueryHandler(
        IClock clock,
        IUserRegistry userRegistry,
        IAltinnAuthorization altinnAuthorization,
        IDialogSearchRepository searchRepository,
        IUser user)
    {
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

        var guiAttachmentCountByDialogIdTask = FetchGuiAttachmentCountByDialogId(dialogIds, cancellationToken);
        var contentByDialogIdTask = FetchContentByDialogId(dialogIds, cancellationToken);
        var endUserContextByDialogIdTask = FetchEndUserContextByDialogId(dialogIds, cancellationToken);
        var seenLogsByDialogIdTask = FetchSeenLogByDialogId(dialogIds, cancellationToken);
        var latestActivitiesByDialogIdTask = FetchLatestActivitiesByDialogId(dialogIds, cancellationToken);
        await Task.WhenAll(
            guiAttachmentCountByDialogIdTask,
            contentByDialogIdTask,
            endUserContextByDialogIdTask,
            seenLogsByDialogIdTask,
            latestActivitiesByDialogIdTask);
        MaskActorIdentifiers(seenLogsByDialogIdTask.Result, latestActivitiesByDialogIdTask.Result);

        var localizationSets = Enumerable.Empty<List<LocalizationDto>>()
            .Concat(contentByDialogIdTask.Result.Values.Select(x => x.ExtendedStatus?.Value))
            .Concat(contentByDialogIdTask.Result.Values.Select(x => x.SenderName?.Value))
            .Concat(contentByDialogIdTask.Result.Values.Select(x => x.Summary?.Value))
            .Concat(contentByDialogIdTask.Result.Values.Select(x => x.Title.Value))
            .Concat(latestActivitiesByDialogIdTask.Result.Values.Select(x => x?.Description));
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
                GuiAttachmentCount = guiAttachmentCountByDialogIdTask.Result.GetValueOrDefault(dialog.Id),
                ExtendedStatus = dialog.ExtendedStatus,
                ExternalReference = dialog.ExternalReference,
                CreatedAt = dialog.CreatedAt,
                UpdatedAt = dialog.UpdatedAt,
                ContentUpdatedAt = dialog.ContentUpdatedAt,
                DueAt = dialog.DueAt,
                Status = dialog.StatusId,
                HasUnopenedContent = dialog.HasUnopenedContent,
                SystemLabel = endUserContextByDialogIdTask.Result[dialog.Id].SystemLabels
                    .FirstOrDefault(SystemLabel.DefaultArchiveBinGroup.Contains),
                IsApiOnly = dialog.IsApiOnly,
                FromServiceOwnerTransmissionsCount = dialog.FromServiceOwnerTransmissionsCount,
                FromPartyTransmissionsCount = dialog.FromPartyTransmissionsCount,
                LatestActivity = latestActivitiesByDialogIdTask.Result.GetValueOrDefault(dialog.Id),
                SeenSinceLastUpdate = seenLogsByDialogIdTask.Result
                    .GetValueOrDefault(dialog.Id)?
                    .Where(x => x.SeenAt >= dialog.UpdatedAt)
                    .GroupBy(x => x.SeenBy.ActorId)
                    .Select(g => g.OrderByDescending(x => x.SeenAt).First())
                    .ToList() ?? [],
                SeenSinceLastContentUpdate = seenLogsByDialogIdTask.Result
                    .GetValueOrDefault(dialog.Id)?
                    .Where(x => x.SeenAt >= dialog.ContentUpdatedAt)
                    .GroupBy(x => x.SeenBy.ActorId)
                    .Select(g => g.OrderByDescending(x => x.SeenAt).First())
                    .ToList() ?? [],
                EndUserContext = endUserContextByDialogIdTask.Result[dialog.Id],
                Content = contentByDialogIdTask.Result[dialog.Id],
            };
        });

        return result;
    }

    private static void MaskActorIdentifiers(Dictionary<Guid, List<DialogSeenLogDto>> seenLogsByDialogId, Dictionary<Guid, DialogActivityDto> latestActivitiesByDialogId)
    {
        foreach (var item in seenLogsByDialogId.Values
                     .SelectMany(x => x)
                     .Select(x => x.SeenBy)
                     .Concat(latestActivitiesByDialogId.Values
                         .Select(x => x.PerformedBy)))
        {
            item.ActorId = IdentifierMasker.GetMaybeMaskedIdentifier(item.ActorId);
        }
    }

    private async Task<Dictionary<Guid, DialogActivityDto>> FetchLatestActivitiesByDialogId(Guid[] dialogIds,
        CancellationToken cancellationToken)
    {
        var result = await _searchRepository.FetchLatestActivitiesByDialogId(dialogIds, cancellationToken);
        return result.ToDictionary(x => x.Key, x => new DialogActivityDto
        {
            Id = x.Value.ActivityId,
            CreatedAt = x.Value.CreatedAt,
            Type = x.Value.Type,
            ExtendedType = x.Value.ExtendedType,
            PerformedBy = new ActorDto
            {
                ActorId = x.Value.PerformedBy.ActorId,
                ActorName = x.Value.PerformedBy.ActorName,
                ActorType = x.Value.PerformedBy.ActorType
            },
            TransmissionId = x.Value.TransmissionId,
            Description = x.Value.Description
                .Select(l => new LocalizationDto
                {
                    LanguageCode = l.LanguageCode,
                    Value = l.Value
                })
                .ToList()
        });
    }

    private async Task<Dictionary<Guid, List<DialogSeenLogDto>>> FetchSeenLogByDialogId(Guid[] dialogIds,
        CancellationToken cancellationToken)
    {
        var currentUserId = _userRegistry.GetCurrentUserId().ExternalIdWithPrefix;
        var result = await _searchRepository.FetchSeenLogByDialogId(dialogIds, currentUserId, cancellationToken);
        return result.ToDictionary(x => x.Key, x => x.Value.Select(seenLog => new DialogSeenLogDto
        {
            SeenAt = seenLog.SeenAt,
            Id = seenLog.SeenLogId,
            IsCurrentEndUser = seenLog.IsCurrentEndUser,
            IsViaServiceOwner = seenLog.IsViaServiceOwner,
            SeenBy = new ActorDto
            {
                ActorType = seenLog.SeenBy.ActorType,
                ActorId = seenLog.SeenBy.ActorId,
                ActorName = seenLog.SeenBy.ActorName
            }
        }).ToList());
    }

    private async Task<Dictionary<Guid, DialogEndUserContextDto>> FetchEndUserContextByDialogId(Guid[] dialogIds,
        CancellationToken cancellationToken)
    {
        var result = await _searchRepository.FetchEndUserContextByDialogId(dialogIds, cancellationToken);
        return result.ToDictionary(x => x.Key, x => new DialogEndUserContextDto
        {
            Revision = x.Value.Revision,
            SystemLabels = x.Value.SystemLabels
        });
    }

    private async Task<Dictionary<Guid, ContentDto>> FetchContentByDialogId(Guid[] dialogIds, CancellationToken cancellationToken)
    {
        var userAuthLevel = _user.GetPrincipal().GetAuthenticationLevel();
        var result = await _searchRepository.FetchContentByDialogId(dialogIds, userAuthLevel, cancellationToken);
        return result.ToDictionary(x => x.Key, x => ToContentDto(x.Value));
        static ContentDto ToContentDto(DataContentDto dataContent)
        {
            return new ContentDto
            {
                Title = ToContentValueDto(dataContent.Title)!,
                Summary = ToContentValueDto(dataContent.Summary),
                ExtendedStatus = ToContentValueDto(dataContent.ExtendedStatus),
                SenderName = ToContentValueDto(dataContent.SenderName),
            };
        }
        static ContentValueDto? ToContentValueDto(DataContentValueDto? dataContent)
        {
            return dataContent is null ? null : new ContentValueDto
            {
                MediaType = dataContent.MediaType,
                Value = dataContent.Value.Select(ToLocalizationDto).ToList()
            };
        }
        static LocalizationDto ToLocalizationDto(DataLocalizationDto data)
        {
            return new LocalizationDto
            {
                LanguageCode = data.LanguageCode,
                Value = data.Value
            };
        }
    }

    private Task<Dictionary<Guid, int>> FetchGuiAttachmentCountByDialogId(Guid[] dialogIds,
        CancellationToken cancellationToken)
    {
        return _searchRepository.FetchGuiAttachmentCountByDialogId(dialogIds, cancellationToken);
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
