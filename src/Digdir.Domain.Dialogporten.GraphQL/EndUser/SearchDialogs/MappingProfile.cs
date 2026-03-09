using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using ApplicationDialogStatus = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogStatus;
using ApplicationSystemLabel = Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities.SystemLabel;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;

internal static class GraphQlMapper
{
    extension(SearchDialogInput source)
    {
        public SearchDialogQuery ToQuery() => new()
        {
            Org = source.Org,
            ServiceResource = source.ServiceResource,
            Party = source.Party,
            ExtendedStatus = source.ExtendedStatus,
            ExternalReference = source.ExternalReference,
            Status = source.Status?.Select(status => status.ToApplication()).ToList(),
            Process = source.Process,
            SystemLabel = source.SystemLabel?.Select(label => (ApplicationSystemLabel.Values)label).ToList(),
            ExcludeApiOnly = source.ExcludeApiOnly,
            CreatedAfter = source.CreatedAfter,
            CreatedBefore = source.CreatedBefore,
            ContentUpdatedAfter = source.ContentUpdatedAfter,
            ContentUpdatedBefore = source.ContentUpdatedBefore,
            UpdatedAfter = source.UpdatedAfter,
            UpdatedBefore = source.UpdatedBefore,
            DueAfter = source.DueAfter,
            DueBefore = source.DueBefore,
            Search = source.Search,
            SearchLanguageCode = source.SearchLanguageCode,
            Limit = source.Limit
        };
    }

    extension(PaginatedList<DialogDto> source)
    {
        public SearchDialogsPayload ToGraphQl() => new()
        {
            Items = source.Items.Select(item => item.ToGraphQl()).ToList(),
            HasNextPage = source.HasNextPage,
            ContinuationToken = source.ContinuationToken,
            OrderBy = []
        };
    }

    extension(ContentDto source)
    {
        public SearchContent ToGraphQl() => new()
        {
            Title = source.Title.ToGraphQl(),
            Summary = source.Summary?.ToGraphQl(),
            SenderName = source.SenderName?.ToGraphQl(),
            ExtendedStatus = source.ExtendedStatus?.ToGraphQl()
        };
    }

    extension(DialogDto source)
    {
        public SearchDialog ToGraphQl() => new()
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
            Status = source.Status.ToGraphQl(),
            HasUnopenedContent = source.HasUnopenedContent,
            IsApiOnly = source.IsApiOnly,
            FromServiceOwnerTransmissionsCount = source.FromServiceOwnerTransmissionsCount,
            FromPartyTransmissionsCount = source.FromPartyTransmissionsCount,
            LatestActivity = source.LatestActivity?.ToGraphQl(),
            Content = source.Content.ToGraphQl(),
            SeenSinceLastUpdate = source.SeenSinceLastUpdate.Select(log => log.ToGraphQl()).ToList(),
            SeenSinceLastContentUpdate = source.SeenSinceLastContentUpdate.Select(log => log.ToGraphQl()).ToList(),
            EndUserContext = source.EndUserContext.ToGraphQl()
        };
    }

    extension(DialogEndUserContextDto source)
    {
        public EndUserContext ToGraphQl() => new()
        {
            Revision = source.Revision,
            SystemLabels = source.SystemLabels.Select(label => (SystemLabel)label).ToList()
        };
    }

    extension(DialogStatus source)
    {
        public ApplicationDialogStatus.Values ToApplication() => source switch
        {
            DialogStatus.NotApplicable => ApplicationDialogStatus.Values.NotApplicable,
            DialogStatus.InProgress => ApplicationDialogStatus.Values.InProgress,
            DialogStatus.Draft => ApplicationDialogStatus.Values.Draft,
            DialogStatus.Awaiting => ApplicationDialogStatus.Values.Awaiting,
            DialogStatus.RequiresAttention => ApplicationDialogStatus.Values.RequiresAttention,
            DialogStatus.Completed => ApplicationDialogStatus.Values.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}
