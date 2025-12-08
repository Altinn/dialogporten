using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
using OneOf;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchNew;

public sealed class SearchDialogQuery : SortablePaginationParameter<SearchDialogQueryOrderDefinition, DialogEntity>, IRequest<SearchDialogResult>, IFeatureMetricServiceResourceIgnoreRequest
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

[GenerateOneOf]
public sealed partial class SearchDialogResult : OneOfBase<PaginatedList<DialogDto>, ValidationError>;

internal sealed class SearchDialogQueryHandler : IRequestHandler<SearchDialogQuery, SearchDialogResult>
{
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IDialogSearchRepository _searchRepository;

    public SearchDialogQueryHandler(
        IUserResourceRegistry userResourceRegistry,
        IAltinnAuthorization altinnAuthorization,
        IDialogSearchRepository searchRepository)
    {
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
        _searchRepository = searchRepository ?? throw new ArgumentNullException(nameof(searchRepository));
    }

    public async Task<SearchDialogResult> Handle(SearchDialogQuery request, CancellationToken cancellationToken)
    {
        // TODO: Get org short code instead of resource ids, and use it as the search driver for SO search
        var orgShortNames = await _userResourceRegistry.GetCurrentUserOrgShortNames(cancellationToken);

        // If the service owner impersonates an end user, we need to filter the dialogs
        // based on the end user's authorization, not the service owner's (which is
        // allowed to see everything about every service resource they own).
        var authorizedResources = request.EndUserId is not null
            ? await _altinnAuthorization.GetAuthorizedResourcesForSearch(
                request.Party ?? [],
                request.ServiceResource ?? [],
                cancellationToken)
            : null;

        // Enduser, serviceOwnerLabels
        var dialogs = await _searchRepository.GetDialogs(
            request.ToGetDialogsQuery(),
            authorizedResources,
            cancellationToken);

        var dialogIds = dialogs.Items
            .Select(x => x.Id)
            .ToArray();

        if (dialogIds.Length == 0)
        {
            return PaginatedList<DialogDto>.CreateEmpty(request);
        }

        var guiAttachmentCountByDialogIdTask = FetchGuiAttachmentCountByDialogId(dialogIds, cancellationToken);
        var contentByDialogIdTask = FetchContentByDialogId(dialogIds, cancellationToken);
        var endUserContextByDialogIdTask = FetchEndUserContextByDialogId(dialogIds, cancellationToken);
        var seenLogsByDialogIdTask = FetchSeenLogByDialogId(dialogIds, request.EndUserId, cancellationToken);
        var latestActivitiesByDialogIdTask = FetchLatestActivitiesByDialogId(dialogIds, cancellationToken);
        var serviceOwnerContextByDialogIdTask = FetchServiceOwnerContextByDialogId(dialogIds, cancellationToken);
        await Task.WhenAll(
            guiAttachmentCountByDialogIdTask,
            contentByDialogIdTask,
            endUserContextByDialogIdTask,
            seenLogsByDialogIdTask,
            latestActivitiesByDialogIdTask,
            serviceOwnerContextByDialogIdTask);

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
                DeletedAt = dialog.DeletedAt,
                Revision = dialog.Revision,
                VisibleFrom = dialog.VisibleFrom,
                // TODO: Is there always a service owner context for each dialog?
                ServiceOwnerContext = serviceOwnerContextByDialogIdTask.Result[dialog.Id]
            };
        });

        return result;
    }

    private async Task<Dictionary<Guid, DialogServiceOwnerContextDto>> FetchServiceOwnerContextByDialogId(Guid[] dialogIds, CancellationToken cancellationToken)
    {
        var result = await _searchRepository.FetchServiceOwnerContextByDialogId(dialogIds, cancellationToken);
        return result.ToDictionary(x => x.Key, x => new DialogServiceOwnerContextDto
        {
            Revision = x.Value.Revision,
            ServiceOwnerLabels = x.Value.Labels
                .Select(label => new ServiceOwnerLabelDto { Value = label })
                .ToList()
        });
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
        string? endUserId,
        CancellationToken cancellationToken)
    {
        var result = await _searchRepository.FetchSeenLogByDialogId(dialogIds, currentUserId: endUserId, cancellationToken);
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
        var result = await _searchRepository.FetchContentByDialogId(dialogIds, userAuthLevel: 0, cancellationToken);
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
    public static GetDialogsQuery ToGetDialogsQuery(this SearchDialogQuery request)
    {
        return new GetDialogsQuery
        {
            VisibleAfter = request.VisibleAfter,
            Deleted = request.Deleted switch
            {
                DeletedFilter.Include => null,
                DeletedFilter.Only => true,
                DeletedFilter.Exclude => false,
                _ => throw new ArgumentOutOfRangeException($"{nameof(request)}.{nameof(request.Deleted)}")
            },
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
            // Org = request.Org,
            Party = request.Party,
            ServiceResource = request.ServiceResource,
            Status = request.Status,
            VisibleBefore = request.VisibleBefore,
            ServiceOwnerLabels = request.ServiceOwnerLabels,
        };
    }
}
