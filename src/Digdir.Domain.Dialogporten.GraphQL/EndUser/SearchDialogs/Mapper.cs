using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using DomainDialogStatus = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogStatus;
using DomainSystemLabel = Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities.SystemLabel;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;

internal static class SearchDialogsMapExtensions
{
    public static SearchDialogQuery ToSearchDialogQuery(this SearchDialogInput source) => new()
    {
        Org = source.Org ?? [],
        ServiceResource = source.ServiceResource ?? [],
        Party = source.Party ?? [],
        ExtendedStatus = source.ExtendedStatus ?? [],
        ExternalReference = source.ExternalReference,
        Status = source.Status?.Select(x => x.MapByName<DomainDialogStatus.Values>()).ToList() ?? [],
        Process = source.Process,
        SystemLabel = source.SystemLabel?.Select(x => x.MapByName<DomainSystemLabel.Values>()).ToList() ?? [],
        ExcludeApiOnly = source.ExcludeApiOnly,
        CreatedAfter = source.CreatedAfter,
        CreatedBefore = source.CreatedBefore,
        ContentUpdatedAfter = source.ContentUpdatedAfter,
        ContentUpdatedBefore = source.ContentUpdatedBefore,
        IsContentSeen = source.IsContentSeen,
        UpdatedAfter = source.UpdatedAfter,
        UpdatedBefore = source.UpdatedBefore,
        DueAfter = source.DueAfter,
        DueBefore = source.DueBefore,
        Search = source.Search,
        SearchLanguageCode = source.SearchLanguageCode,
        Limit = source.Limit
    };

    public static SearchDialogsPayload ToSearchDialogsPayload(this PaginatedList<DialogDto> source) => new()
    {
        Items = source.Items.Select(ToSearchDialog).ToList(),
        HasNextPage = source.HasNextPage,
        ContinuationToken = source.ContinuationToken
    };

    internal static SearchDialog ToSearchDialog(DialogDto source) => new()
    {
        Id = source.Id,
        Org = source.Org,
        ServiceResource = source.ServiceResource,
        ServiceResourceType = source.ServiceResourceType,
        Party = source.Party,
        Progress = source.Progress,
        Process = source.Process,
        PrecedingProcess = source.PrecedingProcess,
        GuiAttachmentCount = source.GuiAttachmentCount,
        ExtendedStatus = source.ExtendedStatus,
        ExternalReference = source.ExternalReference,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        ContentUpdatedAt = source.ContentUpdatedAt,
        DueAt = source.DueAt,
        Status = source.Status.MapByName<DialogStatus>(),
        HasUnopenedContent = source.HasUnopenedContent,
        IsApiOnly = source.IsApiOnly,
        FromServiceOwnerTransmissionsCount = source.FromServiceOwnerTransmissionsCount,
        FromPartyTransmissionsCount = source.FromPartyTransmissionsCount,
        LatestActivity = source.LatestActivity is null ? null : ToActivity(source.LatestActivity),
        Content = source.Content is null ? null! : ToSearchContent(source.Content),
        SeenSinceLastUpdate = source.SeenSinceLastUpdate.Select(ToSeenLog).ToList(),
        SeenSinceLastContentUpdate = source.SeenSinceLastContentUpdate.Select(ToSeenLog).ToList(),
        IsContentSeen = source.IsContentSeen,
        EndUserContext = ToEndUserContext(source.EndUserContext)
    };

    internal static SearchContent ToSearchContent(ContentDto source) => new()
    {
        Title = source.Title.ToGraphQlContentValue(),
        Summary = source.Summary.ToGraphQlContentValueOrNull(),
        SenderName = source.SenderName.ToGraphQlContentValueOrNull(),
        ExtendedStatus = source.ExtendedStatus.ToGraphQlContentValueOrNull()
    };

    internal static EndUserContext ToEndUserContext(DialogEndUserContextDto source) => new()
    {
        Revision = source.Revision,
        SystemLabels = source.SystemLabels.Select(x => x.MapByName<SystemLabel>()).ToList()
    };

    internal static SeenLog ToSeenLog(DialogSeenLogDto source) => new()
    {
        Id = source.Id,
        SeenAt = source.SeenAt,
        SeenBy = source.SeenBy.ToGraphQlActor(),
        IsViaServiceOwner = source.IsViaServiceOwner,
        IsCurrentEndUser = source.IsCurrentEndUser
    };

    internal static Activity ToActivity(DialogActivityDto source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type.MapByName<ActivityType>(),
        TransmissionId = source.TransmissionId,
        PerformedBy = source.PerformedBy.ToGraphQlActor(),
        Description = source.Description.ToGraphQlLocalizations()
    };
}
