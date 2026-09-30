#pragma warning disable CS0618 // Obsolete legacy authorization fields are mapped for backwards compatibility
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;

internal static class DialogByIdMapExtensions
{
    public static Dialog ToDialog(this DialogDto source) => new()
    {
        Id = source.Id,
        Revision = source.Revision,
        Org = source.Org,
        ServiceResource = source.ServiceResource,
        ServiceResourceType = source.ServiceResourceType,
        Party = source.Party,
        Progress = source.Progress,
        Process = source.Process,
        PrecedingProcess = source.PrecedingProcess,
        ExtendedStatus = source.ExtendedStatus,
        ExternalReference = source.ExternalReference,
        DueAt = source.DueAt,
        ExpiresAt = source.ExpiresAt,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        ContentUpdatedAt = source.ContentUpdatedAt,
        DialogToken = source.DialogToken,
        Status = source.Status.MapByName<DialogStatus>(),
        HasUnopenedContent = source.HasUnopenedContent,
        FromServiceOwnerTransmissionsCount = source.FromServiceOwnerTransmissionsCount,
        FromPartyTransmissionsCount = source.FromPartyTransmissionsCount,
        IsApiOnly = source.IsApiOnly,
        Content = source.Content.ToContent(),
        Attachments = source.Attachments.Select(ToAttachment).ToList(),
        ExcludedAttachments = source.ExcludedAttachments?.Select(ToExcludedElement).ToList() ?? [],
        GuiActions = source.GuiActions.Select(ToGuiAction).ToList(),
        ExcludedGuiActions = source.ExcludedGuiActions?.Select(ToExcludedElement).ToList() ?? [],
        ApiActions = source.ApiActions.Select(ToApiAction).ToList(),
        ExcludedApiActions = source.ExcludedApiActions?.Select(ToExcludedElement).ToList() ?? [],
        Activities = source.Activities.Select(ToActivity).ToList(),
        SeenSinceLastUpdate = source.SeenSinceLastUpdate.Select(ToSeenLog).ToList(),
        SeenSinceLastContentUpdate = source.SeenSinceLastContentUpdate.Select(ToSeenLog).ToList(),
        IsContentSeen = source.IsContentSeen,
        Transmissions = source.Transmissions.Select(ToTransmission).ToList(),
        ExcludedTransmissions = source.ExcludedTransmissions?.Select(ToExcludedElement).ToList() ?? [],
        EndUserContext = ToEndUserContext(source.EndUserContext)
    };

    internal static Content ToContent(this ContentDto source) => new()
    {
        Title = source.Title.ToGraphQlContentValue(),
        Summary = source.Summary.ToGraphQlContentValueOrNull(),
        SenderName = source.SenderName.ToGraphQlContentValueOrNull(),
        AdditionalInfo = source.AdditionalInfo.ToGraphQlContentValueOrNull(),
        ExtendedStatus = source.ExtendedStatus.ToGraphQlContentValueOrNull(),
        MainContentReference = source.MainContentReference.ToGraphQlContentValueOrNull()
    };

    internal static EndUserContext ToEndUserContext(DialogEndUserContextDto source) => new()
    {
        Revision = source.Revision,
        SystemLabels = source.SystemLabels.Select(x => x.MapByName<SystemLabel>()).ToList()
    };

    internal static ExcludedElement ToExcludedElement(ExcludedElementDto source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt
    };

    internal static Attachment ToAttachment(DialogAttachmentDto source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName.ToGraphQlLocalizations(),
        Name = source.Name,
        Urls = source.Urls.Select(ToAttachmentUrl).ToList(),
        ExpiresAt = source.ExpiresAt,
        IsAuthorized = source.IsAuthorized
    };

    internal static AttachmentUrl ToAttachmentUrl(DialogAttachmentUrlDto source) => new()
    {
        Id = source.Id,
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType.MapByName<AttachmentUrlConsumer>()
    };

    internal static Attachment ToAttachment(DialogTransmissionAttachmentDto source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName.ToGraphQlLocalizations(),
        Name = source.Name,
        Urls = source.Urls.Select(ToAttachmentUrl).ToList(),
        ExpiresAt = source.ExpiresAt,
        IsAuthorized = source.IsAuthorized
    };

    internal static AttachmentUrl ToAttachmentUrl(DialogTransmissionAttachmentUrlDto source) => new()
    {
        Id = source.Id,
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType.MapByName<AttachmentUrlConsumer>()
    };

    internal static GuiAction ToGuiAction(DialogGuiActionDto source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        Url = source.Url,
        AuthorizationAttribute = source.AuthorizationAttribute,
        IsAuthorized = source.IsAuthorized,
        IsDeleteDialogAction = source.IsDeleteDialogAction,
        Priority = source.Priority.MapByName<GuiActionPriority>(),
        HttpMethod = source.HttpMethod.MapByName<HttpVerb>(),
        Title = source.Title.ToGraphQlLocalizations(),
        Prompt = (source.Prompt ?? []).ToGraphQlLocalizations()
    };

    internal static ApiAction ToApiAction(DialogApiActionDto source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        AuthorizationAttribute = source.AuthorizationAttribute,
        IsAuthorized = source.IsAuthorized,
        Name = source.Name,
        Endpoints = source.Endpoints.Select(ToApiActionEndpoint).ToList()
    };

    internal static ApiActionEndpoint ToApiActionEndpoint(DialogApiActionEndpointDto source) => new()
    {
        Id = source.Id,
        Version = source.Version,
        Url = source.Url,
        HttpMethod = source.HttpMethod.MapByName<HttpVerb>(),
        DocumentationUrl = source.DocumentationUrl,
        RequestSchema = source.RequestSchema,
        ResponseSchema = source.ResponseSchema,
        Deprecated = source.Deprecated,
        SunsetAt = source.SunsetAt
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

    internal static Transmission ToTransmission(DialogTransmissionDto source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        AuthorizationAttribute = source.AuthorizationAttribute,
        IsAuthorized = source.IsAuthorized,
        ExtendedType = source.ExtendedType,
        ExternalReference = source.ExternalReference,
        RelatedTransmissionId = source.RelatedTransmissionId,
        Type = source.Type.MapByName<TransmissionType>(),
        Sender = source.Sender.ToGraphQlActor(),
        IsOpened = source.IsOpened,
        Content = ToTransmissionContent(source.Content),
        Attachments = source.Attachments.Select(ToAttachment).ToList(),
        ExcludedAttachments = source.ExcludedAttachments?.Select(ToExcludedElement).ToList() ?? [],
        NavigationalActions = source.NavigationalActions.Select(ToNavigationalAction).ToList(),
        ExcludedNavigationalActions = source.ExcludedNavigationalActions?.Select(ToExcludedElement).ToList() ?? []
    };

    internal static TransmissionContent ToTransmissionContent(DialogTransmissionContentDto source) => new()
    {
        Title = source.Title.ToGraphQlContentValue(),
        Summary = source.Summary.ToGraphQlContentValueOrNull(),
        ContentReference = source.ContentReference.ToGraphQlContentValueOrNull()
    };

    internal static TransmissionNavigationalAction ToNavigationalAction(DialogTransmissionNavigationalActionDto source) => new()
    {
        Id = source.Id,
        Title = source.Title.ToGraphQlLocalizations(),
        Url = source.Url,
        ExpiresAt = source.ExpiresAt,
        IsAuthorized = source.IsAuthorized
    };
}
