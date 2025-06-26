using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Http;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;

public sealed class DialogDto
{
    /// <summary>
    /// The unique identifier for the dialog in UUIDv7 format.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid Id { get; set; }

    /// <summary>
    ///  An optional key to ensure idempotency in dialog creation. If provided, it allows for the safe re-submission of the same dialog creation request without creating duplicate entries.
    /// </summary>
    public string? IdempotentKey { get; set; }

    /// <summary>
    /// The unique identifier for the revision in UUIDv4 format.
    /// </summary>
    /// <example>a312cb9c-7632-43c2-aa38-69b06aed56ca</example>
    public Guid Revision { get; set; }

    /// <summary>
    /// The service owner code representing the organization (service owner) related to this dialog.
    /// </summary>
    /// <example>ske</example>
    public string Org { get; set; } = null!;

    /// <summary>
    /// The service identifier for the service that the dialog is related to in URN-format.
    /// This corresponds to a service resource in the Altinn Resource Registry.
    /// </summary>
    /// <example>urn:altinn:resource:some-service-identifier</example>
    public string ServiceResource { get; set; } = null!;

    /// <summary>
    /// The ServiceResource type, as defined in Altinn Resource Registry (see ResourceType).
    /// </summary>
    public string ServiceResourceType { get; set; } = null!;

    /// <summary>
    /// The party code representing the organization or person that the dialog belongs to in URN format.
    /// </summary>
    /// <example>
    /// urn:altinn:person:identifier-no:01125512345
    /// urn:altinn:organization:identifier-no:912345678
    /// </example>
    public string Party { get; set; } = null!;

    /// <summary>
    /// Advisory indicator of progress, represented as 1-100 percentage value. 100% representing a dialog that has come
    /// to a natural completion (successful or not).
    /// </summary>
    public int? Progress { get; set; }

    /// <summary>
    /// Optional process identifier used to indicate a business process this dialog belongs to.
    /// </summary>
    public string? Process { get; set; }

    /// <summary>
    /// Optional preceding process identifier to indicate the business process that preceded the process indicated in the "Process" field. Cannot be set without also "Process" being set.
    /// </summary>
    public string? PrecedingProcess { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific indicator of status, typically used to indicate a fine-grained state of
    /// the dialog to further specify the "status" enum.
    ///
    /// Refer to the service-specific documentation provided by the service owner for details on the possible values (if
    /// in use).
    /// </summary>
    public string? ExtendedStatus { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    ///
    /// Refer to the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    public string? ExternalReference { get; set; }

    /// <summary>
    /// If deleted, the date and time when the deletion was performed.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// The timestamp when the dialog will be made visible for authorized end users.
    /// </summary>
    /// <example>2022-12-31T23:59:59Z</example>
    public DateTimeOffset? VisibleFrom { get; set; }

    /// <summary>
    /// The due date for the dialog. Dialogs past due date might be marked as such in frontends but will still be available.
    /// </summary>
    /// <example>2022-12-31T23:59:59Z</example>
    public DateTimeOffset? DueAt { get; set; }

    /// <summary>
    /// The expiration date for the dialog. This is the last date when the dialog is available for the end user.
    ///
    /// After this date is passed, the dialog will be considered expired and no longer available for the end user in any
    /// API. If not supplied, the dialog will be considered to never expire. This field can be changed by the service
    /// owner after the dialog has been created.
    /// </summary>
    /// <example>2022-12-31T23:59:59Z</example>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// The date and time when the dialog was created.
    /// </summary>
    /// <example>2022-12-31T23:59:59Z</example>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the dialog was last updated.
    /// </summary>
    /// <example>2022-12-31T23:59:59Z</example>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// The aggregated status of the dialog.
    /// </summary>
    public DialogStatus.Values Status { get; set; }

    /// <summary>
    /// System defined label used to categorize dialogs.
    /// This is obsolete and will only show; <see cref="SystemLabel.Values.Default"/>, <see cref="SystemLabel.Values.Bin"/> or <see cref="SystemLabel.Values.Archive"/>.
    /// Use <see cref="DialogEndUserContextDto.SystemLabels"/> on <see cref="EndUserContext"/> instead.
    /// </summary>
    [Obsolete($"Use {nameof(EndUserContext)}.{nameof(DialogEndUserContextDto.SystemLabels)} instead.")]
    public SystemLabel.Values SystemLabel { get; set; }

    /// <summary>
    /// Indicates if this dialog is intended for API consumption only and should not be shown in frontends aimed at humans.
    /// </summary>
    public bool IsApiOnly { get; set; }

    /// <summary>
    /// Indicates whether the dialog contains content that has not been viewed or opened by the user yet.
    /// </summary>
    public bool HasUnopenedContent { get; set; }

    /// <summary>
    /// The dialog unstructured text content.
    /// </summary>
    public ContentDto? Content { get; set; }

    /// <summary>
    /// The list of words (tags) that will be used in dialog search queries. Not visible in end-user DTO.
    /// </summary>
    public List<SearchTagDto>? SearchTags { get; set; }

    /// <summary>
    /// The attachments associated with the dialog (on an aggregate level).
    /// </summary>
    public List<DialogAttachmentDto> Attachments { get; set; } = [];

    /// <summary>
    /// The immutable list of transmissions associated with the dialog.
    /// </summary>
    public List<DialogTransmissionDto> Transmissions { get; set; } = [];

    /// <summary>
    /// The GUI actions associated with the dialog. Should be used in browser-based interactive frontends.
    /// </summary>
    public List<DialogGuiActionDto> GuiActions { get; set; } = [];

    /// <summary>
    /// The API actions associated with the dialog. Should be used in specialized, non-browser-based integrations.
    /// </summary>
    public List<DialogApiActionDto> ApiActions { get; set; } = [];

    /// <summary>
    /// An immutable list of activities associated with the dialog.
    /// </summary>
    public List<DialogActivityDto> Activities { get; set; } = [];

    /// <summary>
    /// The list of seen log entries for the dialog newer than the dialog ChangedAt date.
    /// </summary>
    public List<DialogSeenLogDto> SeenSinceLastUpdate { get; set; } = [];

    /// <summary>
    /// Metadata about the dialog owned by the service owner.
    /// </summary>
    public DialogServiceOwnerContextDto ServiceOwnerContext { get; set; } = null!;

    /// <summary>
    /// Metadata about the dialog owned by end-users.
    /// </summary>
    public DialogEndUserContextDto EndUserContext { get; set; } = null!;
}

public sealed class DialogEndUserContextDto
{
    /// <summary>
    /// The unique identifier for the end user context revision in UUIDv4 format.
    /// </summary>
    /// <example>0196fccd-bf48-7d27-bdfc-4ad3b0f3bee5</example>
    public Guid Revision { get; set; }

    /// <summary>
    /// System defined labels used to categorize dialogs.
    /// </summary>
    public List<SystemLabel.Values> SystemLabels { get; set; } = [];
}

public sealed class DialogServiceOwnerContextDto
{
    /// <summary>
    /// A list of labels, not visible in end-user APIs.
    /// </summary>
    public List<DialogServiceOwnerLabelDto> ServiceOwnerLabels { get; set; } = [];

    /// <summary>
    /// The unique identifier for the service owner context revision in UUIDv4 format.
    /// </summary>
    /// <example>0196fccd-bf48-7d27-bdfc-4ad3b0f3bee5</example>
    public Guid Revision { get; set; }
}

public sealed class DialogTransmissionDto
{
    /// <summary>
    /// The unique identifier for the transmission in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The date and time when the transmission was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service
    /// policy, which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    ///
    /// Can also be used to refer to other service policies.
    /// </summary>
    /// <example>
    /// mycustomresource
    /// /* equivalent to the above */
    /// urn:altinn:subresource:mycustomresource
    /// urn:altinn:task:Task_1
    /// /* refer to another service */
    /// urn:altinn:resource:some-other-service-identifier
    /// </example>
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// Flag indicating if the authenticated user supplied in the query is authorized for this transmission.
    /// </summary>
    public bool? IsAuthorized { get; set; }

    /// <summary>
    /// Arbitrary URI/URN describing a service-specific transmission type.
    ///
    /// Refer to the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    public Uri? ExtendedType { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// </summary>
    public string? ExternalReference { get; set; }

    /// <summary>
    /// Reference to any other transmission that this transmission is related to.
    /// </summary>
    public Guid? RelatedTransmissionId { get; set; }

    /// <summary>
    /// The type of transmission.
    /// </summary>
    public DialogTransmissionType.Values Type { get; set; }

    /// <summary>
    /// The actor that sent the transmission.
    /// </summary>
    public ActorDto Sender { get; set; } = null!;

    /// <summary>
    /// The transmission unstructured text content.
    /// </summary>
    public DialogTransmissionContentDto Content { get; set; } = null!;

    /// <summary>
    /// Indicates whether the dialog transmission has been opened.
    /// </summary>
    public bool? IsOpened { get; set; }

    /// <summary>
    /// The transmission-level attachments.
    /// </summary>
    public List<DialogTransmissionAttachmentDto> Attachments { get; set; } = [];
}

public sealed class DialogSeenLogDto
{
    /// <summary>
    /// The unique identifier for the seen log entry in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The timestamp when the dialog revision was seen.
    /// </summary>
    public DateTimeOffset SeenAt { get; set; }

    /// <summary>
    /// The actor that saw the dialog revision.
    /// </summary>
    public ActorDto SeenBy { get; set; } = null!;

    /// <summary>
    /// Flag indicating whether the seen log entry was created via the service owner.
    ///
    /// This is used when the service owner uses the service owner API to implement its own frontend.
    /// </summary>
    public bool? IsViaServiceOwner { get; set; }

    /// <summary>
    /// Flag indicating whether the seen log entry was created by the current end user, if provided in the query.
    /// </summary>
    public bool IsCurrentEndUser { get; set; }
}

public sealed class ContentDto
{
    /// <summary>
    /// The title of the dialog.
    /// </summary>
    public ContentValueDto Title { get; set; } = null!;

    /// <summary>
    /// An optional non-sensitive title of the dialog.
    /// Used for search and list views if the user authorization does not meet the required eIDAS level
    /// </summary>
    public ContentValueDto? NonSensitiveTitle { get; set; }

    /// <summary>
    /// A short summary of the dialog and its current state.
    /// </summary>
    public ContentValueDto? Summary { get; set; }

    /// <summary>
    /// An optional non-sensitive summary of the dialog and its current state.
    /// Used for search and list views if the user authorization does not meet the required eIDAS level
    /// </summary>
    public ContentValueDto? NonSensitiveSummary { get; set; }

    /// <summary>
    /// Overridden sender name. If not supplied, assume "org" as the sender name.
    /// </summary>
    public ContentValueDto? SenderName { get; set; }

    /// <summary>
    /// Additional information about the dialog, this may contain Markdown.
    /// </summary>
    public ContentValueDto? AdditionalInfo { get; set; }

    /// <summary>
    /// Used as the human-readable label used to describe the "ExtendedStatus" field.
    /// </summary>
    public ContentValueDto? ExtendedStatus { get; set; }

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL. Must be HTTPS.
    /// </summary>
    public ContentValueDto? MainContentReference { get; set; }
}

public sealed class DialogTransmissionContentDto
{
    /// <summary>
    /// The transmission title.
    /// </summary>
    public ContentValueDto Title { get; set; } = null!;

    /// <summary>
    /// The transmission summary.
    /// </summary>
    public ContentValueDto? Summary { get; set; }

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL. Must be HTTPS.
    /// </summary>
    public ContentValueDto? ContentReference { get; set; }
}

public sealed class SearchTagDto
{
    /// <summary>
    /// A search tag value.
    /// </summary>
    public string Value { get; set; } = null!;
}

public sealed class DialogServiceOwnerLabelDto
{
    /// <summary>
    /// A label value.
    /// </summary>
    public string Value { get; set; } = null!;
}

public sealed class DialogActivityDto
{
    /// <summary>
    /// The unique identifier for the activity in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The date and time when the activity was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// An arbitrary URI/URN with a service-specific activity type.
    ///
    /// Consult the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    public Uri? ExtendedType { get; set; }

    /// <summary>
    /// The type of activity.
    /// </summary>
    public DialogActivityType.Values Type { get; set; }

    /// <summary>
    /// If the activity is related to a particular transmission, this field will contain the transmission identifier.
    /// </summary>
    public Guid? TransmissionId { get; set; }

    /// <summary>
    /// The actor that performed the activity.
    /// </summary>
    public ActorDto PerformedBy { get; set; } = null!;

    /// <summary>
    /// Unstructured text describing the activity. Only set if the activity type is "Information".
    /// </summary>
    public List<LocalizationDto> Description { get; set; } = [];
}

public sealed class DialogApiActionDto
{
    /// <summary>
    /// The unique identifier for the action in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// String identifier for the action, corresponding to the "action" attributeId used in the XACML service policy,
    /// which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    /// </summary>
    /// <example>write</example>
    public string Action { get; set; } = null!;

    /// <summary>
    /// Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service
    /// policy, which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    ///
    /// Can also be used to refer to other service policies.
    /// </summary>
    /// <example>
    /// mycustomresource
    /// /* equivalent to the above */
    /// urn:altinn:subresource:mycustomresource
    /// urn:altinn:task:Task_1
    /// /* refer to another service */
    /// urn:altinn:resource:some-other-service-identifier
    /// </example>
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// True if the authenticated user (set in the query) is authorized for this action.
    /// </summary>
    public bool? IsAuthorized { get; set; }

    /// <summary>
    /// The logical name of the operation the API action refers to.
    /// </summary>
    /// <example>confirm</example>
    public string? Name { get; set; }

    /// <summary>
    /// The endpoints associated with the action.
    /// </summary>
    public List<DialogApiActionEndpointDto> Endpoints { get; set; } = [];
}

public sealed class DialogApiActionEndpointDto
{
    /// <summary>
    /// The unique identifier for the endpoint in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Arbitrary string indicating the version of the endpoint.
    ///
    /// Consult the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// The fully qualified URL of the API endpoint.
    /// </summary>
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The HTTP method that the endpoint expects for this action.
    /// </summary>
    public HttpVerb.Values HttpMethod { get; set; }

    /// <summary>
    /// Link to service provider documentation for the endpoint. Used for service owners to provide documentation for
    /// integrators. Should be a URL to a human-readable page.
    /// </summary>
    public Uri? DocumentationUrl { get; set; }

    /// <summary>
    /// Link to the request schema for the endpoint. Used by service owners to provide documentation for integrators.
    /// Dialogporten will not validate information on this endpoint.
    /// </summary>
    public Uri? RequestSchema { get; set; }

    /// <summary>
    /// Link to the response schema for the endpoint. Used for service owners to provide documentation for integrators.
    /// Dialogporten will not validate information on this endpoint.
    /// </summary>
    public Uri? ResponseSchema { get; set; }

    /// <summary>
    /// Boolean indicating if the endpoint is deprecated. Integrators should migrate to endpoints with a higher version.
    /// </summary>
    public bool Deprecated { get; set; }

    /// <summary>
    /// Date and time when the service owner has indicated that endpoint will no longer function. Only set if the endpoint
    /// is deprecated. Dialogporten will not enforce this date.
    /// </summary>
    public DateTimeOffset? SunsetAt { get; set; }
}

public sealed class DialogGuiActionDto
{
    /// <summary>
    /// The unique identifier for the action in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The action identifier for the action, corresponding to the "action" attributeId used in the XACML service policy.
    /// </summary>
    public string Action { get; set; } = null!;

    /// <summary>
    /// The fully qualified URL of the action, to which the user will be redirected when the action is triggered.
    /// </summary>
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service
    /// policy, which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    ///
    /// Can also be used to refer to other service policies.
    /// </summary>
    /// <example>
    /// mycustomresource
    /// /* equivalent to the above */
    /// urn:altinn:subresource:mycustomresource
    /// urn:altinn:task:Task_1
    /// /* refer to another service */
    /// urn:altinn:resource:some-other-service-identifier
    /// </example>
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// Whether the user, if supplied in the query, is authorized to perform the action.
    /// </summary>
    public bool? IsAuthorized { get; set; }

    /// <summary>
    /// Indicates whether the action results in the dialog being deleted. Used by frontends to implement custom UX
    /// for delete actions.
    /// </summary>
    public bool IsDeleteDialogAction { get; set; }

    /// <summary>
    /// Indicates a priority for the action, making it possible for frontends to adapt GUI elements based on action
    /// priority.
    /// </summary>
    public DialogGuiActionPriority.Values Priority { get; set; }

    /// <summary>
    /// The HTTP method that the frontend should use when redirecting the user.
    /// </summary>
    public HttpVerb.Values HttpMethod { get; set; }

    /// <summary>
    /// The title of the action, this should be short and in verb form.
    /// </summary>
    public List<LocalizationDto> Title { get; set; } = [];

    /// <summary>
    /// If there should be a prompt asking the user for confirmation before the action is executed,
    /// this field should contain the prompt text.
    /// </summary>
    public List<LocalizationDto>? Prompt { get; set; }
}

public sealed class DialogAttachmentDto
{
    /// <summary>
    /// The unique identifier for the attachment in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The display name of the attachment that should be used in GUIs.
    /// </summary>
    public List<LocalizationDto> DisplayName { get; set; } = [];

    /// <summary>
    /// The URLs associated with the attachment, each referring to a different representation of the attachment.
    /// </summary>
    public List<DialogAttachmentUrlDto> Urls { get; set; } = [];
}

public sealed class DialogAttachmentUrlDto
{
    /// <summary>
    /// The unique identifier for the attachment URL in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The fully qualified URL of the attachment.
    /// </summary>
    /// <example>
    /// https://someendpoint.com/someattachment.pdf
    /// </example>
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The media type of the attachment.
    /// </summary>
    /// <example>
    /// application/pdf
    /// application/zip
    /// </example>
    public string? MediaType { get; set; } = null!;

    /// <summary>
    /// What type of consumer the URL is intended for.
    /// </summary>
    public AttachmentUrlConsumerType.Values ConsumerType { get; set; }
}

public sealed class DialogTransmissionAttachmentDto
{
    /// <summary>
    /// The unique identifier for the attachment in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The display name of the attachment that should be used in GUIs.
    /// </summary>
    public List<LocalizationDto> DisplayName { get; set; } = [];

    /// <summary>
    /// The URLs associated with the attachment, each referring to a different representation of the attachment.
    /// </summary>
    public List<DialogTransmissionAttachmentUrlDto> Urls { get; set; } = [];
}

public sealed class DialogTransmissionAttachmentUrlDto
{
    /// <summary>
    /// The fully qualified URL of the attachment. Will be set to "urn:dialogporten:unauthorized" if the user is
    /// not authorized to access the transmission.
    /// </summary>
    /// <example>
    /// https://someendpoint.com/someattachment.pdf
    /// urn:dialogporten:unauthorized
    /// </example>
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The media type of the attachment.
    /// </summary>
    /// <example>
    /// application/pdf
    /// application/zip
    /// </example>
    public string? MediaType { get; set; } = null!;

    /// <summary>
    /// The type of consumer the URL is intended for.
    /// </summary>
    public AttachmentUrlConsumerType.Values ConsumerType { get; set; }
}
