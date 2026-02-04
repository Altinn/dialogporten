using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureToggle;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.Extensions.Options;
using Old = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using SearchDialogQueryOrderDefinition = Digdir.Domain.Dialogporten.Application.Externals.SearchDialogQueryOrderDefinition;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchNew;

internal sealed class OptimizedServiceOwnerDialogSearchFeatureToggle(IOptionsSnapshot<ApplicationSettings> appSettings) :
    AbstractApplicationFeatureToggle<Old.SearchDialogQuery, Old.SearchDialogResult, SearchDialogQuery,
        SearchDialogResult>
{
    public override bool IsEnabled => appSettings.Value.FeatureToggle.UseOptimizedServiceOwnerDialogSearch;

    protected override SearchDialogQuery ConvertRequest(Old.SearchDialogQuery request) => new()
    {
        Limit = request.Limit,
        Search = request.Search,
        ServiceResource = request.ServiceResource,
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
        Deleted = request.Deleted,
        EndUserId = request.EndUserId,
        ServiceOwnerLabels = request.ServiceOwnerLabels,
        VisibleAfter = request.VisibleAfter,
        VisibleBefore = request.VisibleBefore,
        OrderBy = ToNew(request.OrderBy),
        ContinuationToken = ToNew(request.ContinuationToken),
    };

    protected override Old.SearchDialogResult ConvertResponse(SearchDialogResult response) =>
        response.Match<Old.SearchDialogResult>(
            success => ToOld(success),
            validationError => validationError);

    private static PaginatedList<Old.DialogDto> ToOld(PaginatedList<DialogDto> response) =>
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
            DeletedAt = x.DeletedAt,
            Revision = x.Revision,
            VisibleFrom = x.VisibleFrom,
            SeenSinceLastUpdate = ToOld(x.SeenSinceLastUpdate),
            SeenSinceLastContentUpdate = ToOld(x.SeenSinceLastContentUpdate),
            Content = ToOld(x.Content),
            EndUserContext = ToOld(x.EndUserContext),
            LatestActivity = ToOld(x.LatestActivity),
            ServiceOwnerContext = ToOld(x.ServiceOwnerContext)
        });

    private static Old.DialogServiceOwnerContextDto ToOld(DialogServiceOwnerContextDto x) => new()
    {
        Revision = x.Revision,
        ServiceOwnerLabels = ToOld(x.ServiceOwnerLabels)
    };

    private static List<Old.ServiceOwnerLabelDto> ToOld(List<ServiceOwnerLabelDto> items) => items
        .Select(x => new Old.ServiceOwnerLabelDto { Value = x.Value })
        .ToList();

    private static Old.DialogActivityDto? ToOld(DialogActivityDto? x) =>
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

    private static Old.DialogEndUserContextDto ToOld(DialogEndUserContextDto x) => new()
    {
        Revision = x.Revision,
        SystemLabels = x.SystemLabels
    };

    private static Old.ContentDto? ToOld(ContentDto? x) => x is null ? null : new()
    {
        ExtendedStatus = x.ExtendedStatus,
        SenderName = x.SenderName,
        Summary = x.Summary,
        Title = x.Title,
        NonSensitiveSummary = x.NonSensitiveSummary,
        NonSensitiveTitle = x.NonSensitiveTitle
    };

    private static List<Old.DialogSeenLogDto> ToOld(List<DialogSeenLogDto> arg) => arg
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
