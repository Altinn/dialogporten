using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Http;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

public sealed class UpdateDialogDto
{
    /// <summary>
    /// Advisory indicator of progress, represented as 1-100 percentage value. 100% representing a dialog that has come
    /// to a natural completion (successful or not).
    /// </summary>
    public int? Progress { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific indicator of status, typically used to indicate a fine-grained state of
    /// the dialog to further specify the "status" enum.
    /// </summary>
    public string? ExtendedStatus { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// </summary>
    public string? ExternalReference { get; set; }

    /// <summary>
    /// The timestamp when the dialog should be made visible for authorized end users. If not provided, the dialog will be
    /// immediately available.
    /// </summary>
    /// <example>2022-12-31T23:59:59Z</example>
    public DateTimeOffset? VisibleFrom { get; set; }

    /// <summary>
    /// The due date for the dialog. Dialogs past due date might be marked as such in frontends but will still be available.
    /// </summary>
    /// <example>2022-12-31T23:59:59Z</example>
    public DateTimeOffset? DueAt { get; set; }

    /// <summary>
    /// Optional process identifier used to indicate a business process this dialog belongs to.
    /// </summary>
    public string? Process { get; set; }

    /// <summary>
    /// Optional preceding process identifier to indicate the business process that preceded the process indicated in the "Process" field. Cannot be set without also "Process" being set.
    /// </summary>
    public string? PrecedingProcess { get; set; }

    /// <summary>
    /// The expiration date for the dialog. This is the last date when the dialog is available for the end user.
    ///
    /// After this date is passed, the dialog will be considered expired and no longer available for the end user in any
    /// API. If not supplied, the dialog will be considered to never expire. This field can be changed after creation.
    /// </summary>
    /// <example>2022-12-31T23:59:59Z</example>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// The aggregated status of the dialog.
    /// </summary>
    public DialogStatus.Values Status { get; set; }

    /// <summary>
    /// Indicates if this dialog is intended for API consumption only and should not be shown in frontends aimed at humans.
    /// When true, human-readable content like title and summary are not required.
    /// </summary>
    public bool IsApiOnly { get; set; }

    /// <summary>
    /// The dialog unstructured text content.
    /// </summary>
    public ContentDto Content { get; set; } = null!;

    /// <summary>
    /// A list of words (tags) that will be used in dialog search queries. Not visible in end-user DTO.
    /// </summary>
    public List<SearchTagDto> SearchTags { get; set; } = [];

    /// <summary>
    /// The attachments associated with the dialog (on an aggregate level).
    /// </summary>
    public List<AttachmentDto> Attachments { get; set; } = [];

    /// <summary>
    /// The immutable list of transmissions associated with the dialog. When updating via PUT, any transmissions
    /// added here will be appended to the existing list of transmissions.
    /// </summary>
    public List<TransmissionDto> Transmissions { get; set; } = [];

    /// <summary>
    /// The GUI actions associated with the dialog. Should be used in browser-based interactive frontends.
    /// </summary>
    public List<GuiActionDto> GuiActions { get; set; } = [];

    /// <summary>
    /// The API actions associated with the dialog. Should be used in specialized, non-browser-based integrations.
    /// </summary>
    public List<ApiActionDto> ApiActions { get; set; } = [];

    /// <summary>
    /// An immutable list of activities associated with the dialog. When updating via PUT, any activities added here
    /// will be appended to the existing list of activities.
    /// </summary>
    public List<ActivityDto> Activities { get; set; } = [];
}

public class TransmissionDto
{
    /// <summary>
    /// The UUDIv7 of the action may be provided to support idempotent additions to the list of transmissions.
    /// If not supplied, a new UUIDv7 will be generated.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid? Id { get; set; }

    /// <summary>
    /// If supplied, overrides the creating date and time for the transmission.
    /// If not supplied, the current date /time will be used.
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
    /// Arbitrary URI/URN describing a service-specific transmission type.
    ///
    /// Refer to the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    public Uri? ExtendedType { get; set; }

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
    public TransmissionContentDto Content { get; set; } = null!;

    /// <summary>
    /// The transmission-level attachments.
    /// </summary>
    public List<TransmissionAttachmentDto> Attachments { get; set; } = [];
}

public sealed class TransmissionContentDto
{
    /// <summary>
    /// The transmission title. Must be text/plain.
    /// </summary>
    public ContentValueDto Title { get; set; } = null!;

    /// <summary>
    /// The transmission summary.
    /// </summary>
    public ContentValueDto Summary { get; set; } = null!;

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL. Must be HTTPS.
    /// </summary>
    public ContentValueDto? ContentReference { get; set; }
}

public sealed class ContentDto
{
    /// <summary>
    /// The title of the dialog. Must be text/plain.
    /// </summary>
    public ContentValueDto Title { get; set; } = null!;

    /// <summary>
    /// An optional non-sensitive title of the dialog.
    /// Used for search and list views if the user authorization does not meet the required eIDAS level
    /// </summary>
    public ContentValueDto? NonSensitiveTitle { get; set; }

    /// <summary>
    /// A short summary of the dialog and its current state. Must be text/plain.
    /// </summary>
    public ContentValueDto Summary { get; set; } = null!;

    /// <summary>
    /// An optional non-sensitive summary of the dialog and its current state.
    /// Used for search and list views if the user authorization does not meet the required eIDAS level
    /// </summary>
    public ContentValueDto? NonSensitiveSummary { get; set; }

    /// <summary>
    /// Overridden sender name. If not supplied, assume "org" as the sender name. Must be text/plain if supplied.
    /// </summary>
    public ContentValueDto? SenderName { get; set; }

    /// <summary>
    /// Additional information about the dialog, this may contain Markdown.
    /// </summary>
    public ContentValueDto? AdditionalInfo { get; set; }

    /// <summary>
    /// Used as the human-readable label used to describe the "ExtendedStatus" field. Must be text/plain.
    /// </summary>
    public ContentValueDto? ExtendedStatus { get; set; }

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL. Must be HTTPS.
    /// </summary>
    public ContentValueDto? MainContentReference { get; set; }
}

public sealed class SearchTagDto
{
    /// <summary>
    /// A search tag value.
    /// </summary>
    public string Value { get; set; } = null!;
}

public class ActivityDto
{
    /// <summary>
    /// The UUDIv7 of the action may be provided to support idempotent additions to the list of activities.
    /// If not supplied, a new UUIDv7 will be generated.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid? Id { get; set; }

    /// <summary>
    /// If supplied, overrides the creating date and time for the transmission.
    /// If not supplied, the current date /time will be used.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Arbitrary URI/URN describing a service-specific transmission type.
    /// </summary>
    public Uri? ExtendedType { get; set; }

    /// <summary>
    /// The type of transmission.
    /// </summary>
    public DialogActivityType.Values Type { get; set; }

    /// <summary>
    /// If the activity is related to a particular transmission, this field will contain the transmission identifier.
    /// Must be present in the request body.
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

public sealed class ApiActionDto
{
    /// <summary>
    /// A self-defined UUIDv7 may be provided to support idempotent additions of Api Actions. If not provided, a new UUIDv7 will be generated.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid? Id { get; set; }

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
    /// The logical name of the operation the API action refers to.
    /// </summary>
    /// <example>confirm</example>
    public string? Name { get; set; }

    /// <summary>
    /// The endpoints associated with the action.
    /// </summary>
    public List<ApiActionEndpointDto> Endpoints { get; set; } = [];
}

public sealed class ApiActionEndpointDto
{
    /// <summary>
    /// A UUIDv7 used for merging existing data, unknown IDs will be ignored as this entity does not support user-defined IDs.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid? Id { get; set; }

    /// <summary>
    /// Arbitrary string indicating the version of the endpoint.
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
    /// Link to documentation for the endpoint, providing documentation for integrators. Should be a URL to a
    /// human-readable page.
    /// </summary>
    public Uri? DocumentationUrl { get; set; }

    /// <summary>
    /// Link to the request schema for the endpoint. Used to provide documentation for integrators.
    /// Dialogporten will not validate information on this endpoint.
    /// </summary>
    public Uri? RequestSchema { get; set; }

    /// <summary>
    /// Link to the response schema for the endpoint. Used to provide documentation for integrators.
    /// Dialogporten will not validate information on this endpoint.
    /// </summary>
    public Uri? ResponseSchema { get; set; }

    /// <summary>
    /// Boolean indicating if the endpoint is deprecated.
    /// </summary>
    public bool Deprecated { get; set; }

    /// <summary>
    /// Date and time when the endpoint will no longer function. Only set if the endpoint is deprecated. Dialogporten
    /// will not enforce this date.
    /// </summary>
    public DateTimeOffset? SunsetAt { get; set; }
}

public sealed class GuiActionDto
{
    /// <summary>
    /// A self-defined UUIDv7 may be provided to support idempotent additions of Gui Actions. If not provided, a new UUIDv7 will be generated.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid? Id { get; set; }

    /// <summary>
    /// The action identifier for the action, corresponding to the "action" attributeId used in the XACML service policy.
    /// </summary>
    public string Action { get; set; } = null!;

    /// <summary>
    /// The fully qualified URL of the action, to which the user will be redirected when the action is triggered. Will be set to
    /// "urn:dialogporten:unauthorized" if the user is not authorized to perform the action.
    /// </summary>
    /// <example>
    /// urn:dialogporten:unauthorized
    /// https://someendpoint.com/gui/some-service-instance-id
    /// </example>
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
    /// Indicates whether the action results in the dialog being deleted. Used by frontends to implement custom UX
    /// for delete actions.
    /// </summary>
    public bool IsDeleteDialogAction { get; set; }

    /// <summary>
    /// The HTTP method that the frontend should use when redirecting the user.
    /// </summary>
    public HttpVerb.Values? HttpMethod { get; set; } = HttpVerb.Values.GET;

    /// <summary>
    /// Indicates a priority for the action, making it possible for frontends to adapt GUI elements based on action
    /// priority.
    /// </summary>
    public DialogGuiActionPriority.Values Priority { get; set; }

    /// <summary>
    /// The title of the action, this should be short and in verb form. Must be text/plain.
    /// </summary>
    public List<LocalizationDto> Title { get; set; } = [];

    /// <summary>
    /// If there should be a prompt asking the user for confirmation before the action is executed,
    /// this field should contain the prompt text.
    /// </summary>
    public List<LocalizationDto>? Prompt { get; set; }
}

public sealed class AttachmentDto
{
    /// <summary>
    /// A self-defined UUIDv7 may be provided to support idempotent additions of attachments. If not provided, a new UUIDv7 will be generated.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid? Id { get; set; }

    /// <summary>
    /// The display name of the attachment that should be used in GUIs.
    /// </summary>
    public List<LocalizationDto> DisplayName { get; set; } = [];

    /// <summary>
    /// The URLs associated with the attachment, each referring to a different representation of the attachment.
    /// </summary>
    public List<AttachmentUrlDto> Urls { get; set; } = [];
}

public sealed class AttachmentUrlDto
{
    /// <summary>
    /// A UUIDv7 used for merging existing data, unknown IDs will be ignored as this entity does not support user-defined IDs.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid? Id { get; set; }

    /// <summary>
    /// The fully qualified URL of the attachment.
    /// </summary>
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

public sealed class TransmissionAttachmentDto
{
    /// <summary>
    /// A self-defined UUIDv7 may be provided to support idempotent additions of transmission attachments. If not provided, a new UUIDv7 will be generated.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid? Id { get; set; }

    /// <summary>
    /// The display name of the attachment that should be used in GUIs.
    /// </summary>
    public List<LocalizationDto> DisplayName { get; set; } = [];

    /// <summary>
    /// The URLs associated with the attachment, each referring to a different representation of the attachment.
    /// </summary>
    public List<TransmissionAttachmentUrlDto> Urls { get; set; } = [];
}

public sealed class TransmissionAttachmentUrlDto
{
    /// <summary>
    /// The fully qualified URL of the attachment.
    /// </summary>
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
