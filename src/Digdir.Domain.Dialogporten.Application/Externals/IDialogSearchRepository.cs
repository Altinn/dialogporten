using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IDialogSearchRepository
{
    Task UpsertFreeTextSearchIndex(Guid dialogId, CancellationToken cancellationToken);
    Task<int> SeedFullAsync(bool resetExisting, CancellationToken ct);
    Task<int> SeedSinceAsync(DateTimeOffset since, bool resetMatching, CancellationToken ct);
    Task<int> SeedStaleAsync(bool resetMatching, CancellationToken ct);
    Task<int> WorkBatchAsync(int batchSize, long workMemBytes, bool staleFirst, CancellationToken ct);
    Task<DialogSearchReindexProgress> GetProgressAsync(CancellationToken ct);
    Task OptimizeIndexAsync(CancellationToken ct);
    Task<PaginatedList<DialogEntity>> GetDialogs(
        GetDialogsQuery query,
        DialogSearchAuthorizationResult authorizedResources,
        CancellationToken cancellationToken);

    Task<Dictionary<Guid, int>> FetchGuiAttachmentCountByDialogId(Guid[] dialogIds,
        CancellationToken cancellationToken);

    Task<Dictionary<Guid, DataContentDto>> FetchContentByDialogId(Guid[] dialogIds,
        int userAuthLevel,
        CancellationToken cancellationToken);

    Task<Dictionary<Guid, DataDialogEndUserContextDto>> FetchEndUserContextByDialogId(
        Guid[] dialogIds,
        CancellationToken cancellationToken);

    Task<Dictionary<Guid, List<DataDialogSeenLogDto>>> FetchSeenLogByDialogId(
        Guid[] dialogIds,
        string currentUserId,
        CancellationToken cancellationToken);

    Task<Dictionary<Guid, DataDialogActivityDto>> FetchLatestActivitiesByDialogId(
        Guid[] dialogIds,
        CancellationToken cancellationToken);
}

public sealed class DialogSearchReindexProgress
{
    public long Total { get; init; }
    public long Pending { get; init; }
    public long Processing { get; init; }
    public long Done { get; init; }
}

public sealed class SearchDialogQueryOrderDefinition : IOrderDefinition<DialogEntity>
{
    public static IOrderOptions<DialogEntity> Configure(IOrderOptionsBuilder<DialogEntity> options) =>
        options.AddId(x => x.Id)
            .AddDefault("createdAt", x => x.CreatedAt)
            .AddOption("updatedAt", x => x.UpdatedAt)
            .AddOption("contentUpdatedAt", x => x.ContentUpdatedAt)
            .AddOption("dueAt", x => x.DueAt)
            .AddOption("searchRank", x => x.Status)
            .Build();
}

public sealed class GetDialogsQuery
{
    public required bool? Deleted { get; set; }
    public OrderSet<SearchDialogQueryOrderDefinition, DialogEntity>? OrderBy { get; set; }
    public IContinuationTokenSet? ContinuationToken { get; set; }
    public int Limit { get; set; } = 100;

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

    public DateTimeOffset? VisibleAfter { get; set; }
    public DateTimeOffset? ExpiresBefore { get; set; }
}

public sealed record DataContentDto(DataContentValueDto Title, DataContentValueDto? Summary, DataContentValueDto? ExtendedStatus, DataContentValueDto? SenderName);
public sealed record DataContentValueDto(DialogContentType.Values TypeId, string MediaType, List<DataLocalizationDto> Value);
public sealed record DataLocalizationDto(string LanguageCode, string Value);
public sealed record DataDialogEndUserContextDto(Guid Revision, List<SystemLabel.Values> SystemLabels);
public sealed record DataDialogSeenLogDto(Guid SeenLogId, Guid DialogId, DateTimeOffset SeenAt, bool IsViaServiceOwner, bool IsCurrentEndUser, DataActorDto SeenBy);
public sealed record DataActorDto(ActorType.Values ActorType, string? ActorId, string? ActorName);
public sealed record DataDialogActivityDto(Guid ActivityId, DateTimeOffset? CreatedAt, DialogActivityType.Values Type, Uri? ExtendedType, Guid? TransmissionId, DataActorDto PerformedBy, List<DataLocalizationDto> Description);
