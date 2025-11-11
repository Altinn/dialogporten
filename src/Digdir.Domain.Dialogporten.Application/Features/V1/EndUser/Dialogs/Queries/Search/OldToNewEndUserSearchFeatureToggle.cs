using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureToggle;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Old = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchOld;
using New = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;

internal sealed class OldToNewEndUserSearchFeatureToggle :
    AbstractApplicationFeatureToggle<Old.SearchDialogQuery, Old.SearchDialogResult, New.SearchDialogQuery, New.SearchDialogResult>
{
    public override bool IsEnabled => false;

    protected override New.SearchDialogQuery ConvertRequest(Old.SearchDialogQuery request) => new()
    {
        Org = request.Org,
        Limit = request.Limit,
        Search = request.Search,
        ServiceResource = request.ServiceResource,
        AcceptedLanguages = request.AcceptedLanguages,
        ContentUpdatedAfter = request.ContentUpdatedAfter,
        ContentUpdatedBefore = request.ContentUpdatedBefore,
        CreatedAfter = request.CreatedAfter,
        CreatedBefore = request.CreatedBefore,
        DueAfter = request.DueAfter,
        DueBefore = request.DueBefore,
        ExcludeApiOnly = request.ExcludeApiOnly,
        ExtendedStatus = request.ExtendedStatus,
        ExternalReference = request.ExternalReference,
        Party = request.Party,
        Process = request.Process,
        SearchLanguageCode = request.SearchLanguageCode,
        Status = request.Status,
        UpdatedAfter = request.UpdatedAfter,
        UpdatedBefore = request.UpdatedBefore,
        SystemLabel = request.SystemLabel,
        OrderBy = ToNew(request.OrderBy),
        ContinuationToken = ToNew(request.ContinuationToken),
    };

    protected override Old.SearchDialogResult ConvertResponse(New.SearchDialogResult response) =>
        response.Match<Old.SearchDialogResult>(
            success => ToOld(success),
            validationError => validationError,
            forbidden => forbidden);

    private static PaginatedList<Old.DialogDto> ToOld(PaginatedList<New.DialogDto> response) =>
        response.ConvertTo(x => new Old.DialogDto
        {
            ServiceResource = x.ServiceResource,
            ExternalReference = x.ExternalReference,
            Org = x.Org,
            Party = x.Party,
            Status = x.Status,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            DueAt = x.DueAt,
            ContentUpdatedAt = x.ContentUpdatedAt,
            ExtendedStatus = x.ExtendedStatus,
            Process = x.Process,
            FromPartyTransmissionsCount = x.FromPartyTransmissionsCount,
            FromServiceOwnerTransmissionsCount = x.FromServiceOwnerTransmissionsCount,
            GuiAttachmentCount = x.GuiAttachmentCount,
            HasUnopenedContent = x.HasUnopenedContent,
            Id = x.Id,
            IsApiOnly = x.IsApiOnly,
            PrecedingProcess = x.PrecedingProcess,
            Progress = x.Progress,
            ServiceResourceType = x.ServiceResourceType,
            SystemLabel = x.SystemLabel,
            SeenSinceLastUpdate = ToOld(x.SeenSinceLastUpdate),
            SeenSinceLastContentUpdate = ToOld(x.SeenSinceLastContentUpdate),
            Content = ToOld(x.Content),
            EndUserContext = ToOld(x.EndUserContext),
            LatestActivity = ToOld(x.LatestActivity)
        });

    private static Old.DialogActivityDto? ToOld(New.DialogActivityDto? x) =>
        x is null ? null : new Old.DialogActivityDto
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt,
            Description = x.Description,
            ExtendedType = x.ExtendedType,
            PerformedBy = x.PerformedBy,
            TransmissionId = x.TransmissionId,
            Type = x.Type
        };

    private static Old.DialogEndUserContextDto ToOld(New.DialogEndUserContextDto x) => new()
    {
        Revision = x.Revision,
        SystemLabels = x.SystemLabels
    };

    private static Old.ContentDto ToOld(New.ContentDto x) => new()
    {
        ExtendedStatus = x.ExtendedStatus,
        SenderName = x.SenderName,
        Summary = x.Summary,
        Title = x.Title
    };

    private static List<Old.DialogSeenLogDto> ToOld(List<New.DialogSeenLogDto> arg) => arg
        .Select(x => new Old.DialogSeenLogDto
        {
            Id = x.Id,
            IsCurrentEndUser = x.IsCurrentEndUser,
            IsViaServiceOwner = x.IsViaServiceOwner,
            SeenAt = x.SeenAt,
            SeenBy = x.SeenBy
        })
        .ToList();

    private static OrderSet<SearchDialogQueryOrderDefinition, DialogEntity>? ToNew(
        OrderSet<Old.SearchDialogQueryOrderDefinition, Old.IntermediateDialogDto>? x) => x is null ? null
        : !OrderSet<SearchDialogQueryOrderDefinition, DialogEntity>.TryParse(x.GetOrderString(), out var order)
            ? throw new InvalidOperationException("Could not convert order set.")
            : order;

    private static ContinuationTokenSet<SearchDialogQueryOrderDefinition, DialogEntity>? ToNew(
        ContinuationTokenSet<Old.SearchDialogQueryOrderDefinition, Old.IntermediateDialogDto>? x) => x is null ? null
        : !ContinuationTokenSet<SearchDialogQueryOrderDefinition, DialogEntity>.TryParse(x.Raw, out var ct)
            ? throw new InvalidOperationException("Could not convert continuation token set.")
            : ct;
}
