using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;

[InterfaceType("DialogByIdError")]
public interface IDialogByIdError
{
    string Message { get; set; }
}

public sealed class DialogByIdNotFound : IDialogByIdError
{
    public string Message { get; set; } = null!;
}

public sealed class DialogByIdDeleted : IDialogByIdError
{
    public string Message { get; set; } = null!;
}

public sealed class DialogByIdNotVisible : IDialogByIdError
{
    public string Message { get; set; } = null!;
    public DateTimeOffset VisibleFrom { get; set; }
}

public sealed class DialogByIdForbidden : IDialogByIdError
{
    public string Message { get; set; } = "Forbidden";
}

public sealed class DialogByIdForbiddenAuthLevelTooLow : IDialogByIdError
{
    public string Message { get; set; } = Constants.AltinnAuthLevelTooLow;
}

public sealed class DialogByIdPayload
{
    public Dialog? Dialog { get; set; }
    public List<IDialogByIdError> Errors { get; set; } = [];
}

public sealed class Dialog
{
    [GraphQLDescription("The unique identifier for the dialog in UUIDv7 format. Example: 01913cd5-784f-7d3b-abef-4c77b1f0972d")]
    public Guid Id { get; set; }

    [GraphQLDescription("The unique identifier for the revision in UUIDv4 format. Example: a312cb9c-7632-43c2-aa38-69b06aed56ca")]
    public Guid Revision { get; set; }

    [GraphQLDescription("The service owner code representing the organization (service owner) related to this dialog. Example: ske")]
    public string Org { get; set; } = null!;

    [GraphQLDescription("The service identifier for the service that the dialog is related to in URN-format. This corresponds to a service resource in the Altinn Resource Registry. Example: urn:altinn:resource:some-service-identifier")]
    public string ServiceResource { get; set; } = null!;

    [GraphQLDescription("The ServiceResource type, as defined in Altinn Resource Registry (see ResourceType).")]
    public string ServiceResourceType { get; set; } = null!;

    [GraphQLDescription("The party code representing the organization or person that the dialog belongs to in URN format. Example: urn:altinn:person:identifier-no:01125512345, urn:altinn:organization:identifier-no:912345678")]
    public string Party { get; set; } = null!;

    [GraphQLDescription("Advisory indicator of progress, represented as 1-100 percentage value. 100% representing a dialog that has come to a natural completion (successful or not).")]
    public int? Progress { get; set; }

    [GraphQLDescription("Optional process identifier used to indicate a business process this dialog belongs to.")]
    public string? Process { get; set; }

    [GraphQLDescription("Optional preceding process identifier to indicate the business process that preceded the process indicated in the 'Process' field. Cannot be set without also 'Process' being set.")]
    public string? PrecedingProcess { get; set; }

    [GraphQLDescription("Arbitrary string with a service-specific indicator of status, typically used to indicate a fine-grained state of the dialog to further specify the 'status' enum. Refer to the service-specific documentation provided by the service owner for details on the possible values (if in use).")]
    public string? ExtendedStatus { get; set; }

    [GraphQLDescription("Arbitrary string with a service-specific reference to an external system or service. Refer to the service-specific documentation provided by the service owner for details (if in use).")]
    public string? ExternalReference { get; set; }

    [GraphQLDescription("The due date for the dialog. Dialogs past due date might be marked as such in frontends but will still be available. Example: 2022-12-31T23:59:59Z")]
    public DateTimeOffset? DueAt { get; set; }

    [GraphQLDescription("The expiration date for the dialog. This is the last date when the dialog is available for the end user. After this date is passed, the dialog will be considered expired and no longer available for the end user in any API. If not supplied, the dialog will be considered to never expire. This field can be changed by the service owner after the dialog has been created. Example: 2022-12-31T23:59:59Z")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [GraphQLDescription("The date and time when the dialog was created. Example: 2022-12-31T23:59:59Z")]
    public DateTimeOffset CreatedAt { get; set; }

    [GraphQLDescription("The date and time when the dialog was last updated. Example: 2022-12-31T23:59:59Z")]
    public DateTimeOffset UpdatedAt { get; set; }

    [GraphQLDescription("The date and time when the dialog content was last updated. Example: 2022-12-31T23:59:59Z")]
    public DateTimeOffset ContentUpdatedAt { get; set; }

    [GraphQLDescription("The dialog token. May be used (if supported) against external URLs referred to in this dialog's apiActions, transmissions or attachments. It should also be used for front-channel embeds.")]
    public string? DialogToken { get; set; }

    [GraphQLDescription("The aggregated status of the dialog.")]
    public DialogStatus Status { get; set; }

    [GraphQLDescription("Indicates whether the dialog contains content that has not been viewed or opened by the user yet.")]
    public bool HasUnopenedContent { get; set; }

    [GraphQLDescription("The number of transmissions sent by the service owner")]
    public int FromServiceOwnerTransmissionsCount { get; set; }

    [GraphQLDescription("The number of transmissions sent by a party representative")]
    public int FromPartyTransmissionsCount { get; set; }

    [GraphQLDescription("Indicates if this dialog is intended for API consumption only and should not be shown in frontends aimed at humans")]
    public bool IsApiOnly { get; set; }

    [GraphQLDescription("The dialog unstructured text content.")]
    public Content Content { get; set; } = null!;

    [GraphQLDescription("The attachments associated with the dialog (on an aggregate level).")]
    public List<Attachment> Attachments { get; set; } = [];

    [GraphQLDescription("The GUI actions associated with the dialog. Should be used in browser-based interactive frontends.")]
    public List<GuiAction> GuiActions { get; set; } = [];

    [GraphQLDescription("The API actions associated with the dialog. Should be used in specialized, non-browser-based integrations.")]
    public List<ApiAction> ApiActions { get; set; } = [];

    [GraphQLDescription("An immutable list of activities associated with the dialog.")]
    public List<Activity> Activities { get; set; } = [];

    [GraphQLDescription("The list of seen log entries for the dialog newer than the dialog UpdatedAt date.")]
    public List<SeenLog> SeenSinceLastUpdate { get; set; } = [];

    [GraphQLDescription("The list of seen log entries for the dialog newer than the dialog ContentUpdatedAt date.")]
    public List<SeenLog> SeenSinceLastContentUpdate { get; set; } = [];

    [GraphQLDescription("The immutable list of transmissions associated with the dialog.")]
    public List<Transmission> Transmissions { get; set; } = [];

    [GraphQLDescription("Metadata about the dialog owned by end-users.")]
    public EndUserContext EndUserContext { get; set; } = null!;
}

public sealed class Transmission
{
    [GraphQLDescription("The unique identifier for the transmission in UUIDv7 format.")]
    public Guid Id { get; set; }

    [GraphQLDescription("An optional key to ensure idempotency in transmission creation. If provided, it allows for the safe re-submission of the same transmission creation request without creating duplicate entries.")]
    public string? IdempotentKey { get; set; }

    [GraphQLDescription("The date and time when the transmission was created.")]
    public DateTimeOffset CreatedAt { get; set; }

    [GraphQLDescription("Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service policy, which by default is the policy belonging to the service referred to by 'serviceResource' in the dialog. Can also be used to refer to other service policies. Example: mycustomresource, urn:altinn:subresource:mycustomresource, urn:altinn:task:Task_1, urn:altinn:resource:some-other-service-identifier")]
    public string? AuthorizationAttribute { get; set; }

    [GraphQLDescription("Flag indicating if the authenticated user is authorized for this transmission. If not, embedded content and the attachments will not be available.")]
    public bool IsAuthorized { get; set; }

    [GraphQLDescription("Arbitrary URI/URN describing a service-specific transmission type. Refer to the service-specific documentation provided by the service owner for details (if in use).")]
    public Uri? ExtendedType { get; set; }

    [GraphQLDescription("Arbitrary string with a service-specific reference to an external system or service.")]
    public string? ExternalReference { get; set; }

    [GraphQLDescription("Reference to any other transmission that this transmission is related to.")]
    public Guid? RelatedTransmissionId { get; set; }

    [GraphQLDescription("The type of transmission.")]
    public TransmissionType Type { get; set; }

    [GraphQLDescription("The actor that sent the transmission.")]
    public Actor Sender { get; set; } = null!;

    [GraphQLDescription("Indicates whether the dialog transmission has been opened.")]
    public bool IsOpened { get; set; }

    [GraphQLDescription("The transmission unstructured text content.")]
    public TransmissionContent Content { get; set; } = null!;

    [GraphQLDescription("The transmission-level attachments.")]
    public List<Attachment> Attachments { get; set; } = [];
}

public enum TransmissionType
{
    [GraphQLDescription("For general information, not related to any submissions")]
    Information = 1,

    [GraphQLDescription("Feedback/receipt accepting a previous submission")]
    Acceptance = 2,

    [GraphQLDescription("Feedback/error message rejecting a previous submission")]
    Rejection = 3,

    [GraphQLDescription("Question/request for more information")]
    Request = 4,

    [GraphQLDescription("Critical information about the process")]
    Alert = 5,

    [GraphQLDescription("Information about a formal decision ('resolution')")]
    Decision = 6,

    [GraphQLDescription("A normal submission of some information/form")]
    Submission = 7,

    [GraphQLDescription("A submission correcting/overriding some previously submitted information")]
    Correction = 8
}

public sealed class Content
{
    [GraphQLDescription("The title of the dialog.")]
    public ContentValue Title { get; set; } = null!;

    [GraphQLDescription("A short summary of the dialog and its current state.")]
    public ContentValue? Summary { get; set; }

    [GraphQLDescription("Overridden sender name. If not supplied, assume 'org' as the sender name.")]
    public ContentValue? SenderName { get; set; }

    [GraphQLDescription("Additional information about the dialog, this may contain Markdown.")]
    public ContentValue? AdditionalInfo { get; set; }

    [GraphQLDescription("Used as the human-readable label used to describe the 'ExtendedStatus' field.")]
    public ContentValue? ExtendedStatus { get; set; }

    [GraphQLDescription("Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL.")]
    public ContentValue? MainContentReference { get; set; }
}

public sealed class TransmissionContent
{
    [GraphQLDescription("The transmission title.")]
    public ContentValue Title { get; set; } = null!;

    [GraphQLDescription("The transmission summary.")]
    public ContentValue? Summary { get; set; }

    [GraphQLDescription("Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL.")]
    public ContentValue? ContentReference { get; set; }
}

public sealed class ApiAction
{
    [GraphQLDescription("The unique identifier for the action in UUIDv7 format.")]
    public Guid Id { get; set; }

    [GraphQLDescription("String identifier for the action, corresponding to the 'action' attributeId used in the XACML service policy, which by default is the policy belonging to the service referred to by 'serviceResource' in the dialog.")]
    public string Action { get; set; } = null!;

    [GraphQLDescription("Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service policy, which by default is the policy belonging to the service referred to by 'serviceResource' in the dialog. Can also be used to refer to other service policies.")]
    public string? AuthorizationAttribute { get; set; }

    [GraphQLDescription("True if the authenticated user is authorized for this action. If not, the action will not be available and all endpoints will be replaced with a fixed placeholder.")]
    public bool IsAuthorized { get; set; }

    [GraphQLDescription("The logical name of the operation the API action refers to.")]
    public string? Name { get; set; }

    [GraphQLDescription("The endpoints associated with the action.")]
    public List<ApiActionEndpoint> Endpoints { get; set; } = [];
}

// ReSharper disable InconsistentNaming
public enum HttpVerb
{
    GET = 1,
    POST = 2,
    PUT = 3,
    PATCH = 4,
    DELETE = 5,
    HEAD = 6,
    OPTIONS = 7,
    TRACE = 8,
    CONNECT = 9
}

public sealed class ApiActionEndpoint
{
    [GraphQLDescription("The unique identifier for the endpoint in UUIDv7 format.")]
    public Guid Id { get; set; }

    [GraphQLDescription("Arbitrary string indicating the version of the endpoint. Consult the service-specific documentation provided by the service owner for details (if in use).")]
    public string? Version { get; set; }

    [GraphQLDescription("The fully qualified URL of the API endpoint. Will be set to 'urn:dialogporten:unauthorized' if the user is not authorized to perform the action.")]
    public Uri Url { get; set; } = null!;

    [GraphQLDescription("The HTTP method that the endpoint expects for this action.")]
    public HttpVerb HttpMethod { get; set; }

    [GraphQLDescription("Link to service provider documentation for the endpoint. Used for service owners to provide documentation for integrators. Should be a URL to a human-readable page.")]
    public Uri? DocumentationUrl { get; set; }

    [GraphQLDescription("Link to the request schema for the endpoint. Used by service owners to provide documentation for integrators. Dialogporten will not validate information on this endpoint.")]
    public Uri? RequestSchema { get; set; }

    [GraphQLDescription("Link to the response schema for the endpoint. Used for service owners to provide documentation for integrators. Dialogporten will not validate information on this endpoint.")]
    public Uri? ResponseSchema { get; set; }

    [GraphQLDescription("Boolean indicating if the endpoint is deprecated. Integrators should migrate to endpoints with a higher version.")]
    public bool Deprecated { get; set; }

    [GraphQLDescription("Date and time when the service owner has indicated that endpoint will no longer function. Only set if the endpoint is deprecated. Dialogporten will not enforce this date.")]
    public DateTimeOffset? SunsetAt { get; set; }
}

public sealed class GuiAction
{
    [GraphQLDescription("The unique identifier for the action in UUIDv7 format.")]
    public Guid Id { get; set; }

    [GraphQLDescription("The action identifier for the action, corresponding to the 'action' attributeId used in the XACML service policy.")]
    public string Action { get; set; } = null!;

    [GraphQLDescription("The fully qualified URL of the action, to which the user will be redirected when the action is triggered. Will be set to 'urn:dialogporten:unauthorized' if the user is not authorized to perform the action.")]
    public Uri Url { get; set; } = null!;

    [GraphQLDescription("Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service policy, which by default is the policy belonging to the service referred to by 'serviceResource' in the dialog. Can also be used to refer to other service policies.")]
    public string? AuthorizationAttribute { get; set; }

    [GraphQLDescription("Whether the user is authorized to perform the action.")]
    public bool IsAuthorized { get; set; }

    [GraphQLDescription("Indicates whether the action results in the dialog being deleted. Used by frontends to implement custom UX for delete actions.")]
    public bool IsDeleteDialogAction { get; set; }

    [GraphQLDescription("Indicates a priority for the action, making it possible for frontends to adapt GUI elements based on action priority.")]
    public GuiActionPriority Priority { get; set; }

    [GraphQLDescription("The HTTP method that the frontend should use when redirecting the user.")]
    public HttpVerb HttpMethod { get; set; }

    [GraphQLDescription("The title of the action, this should be short and in verb form.")]
    public List<Localization> Title { get; set; } = [];

    [GraphQLDescription("If there should be a prompt asking the user for confirmation before the action is executed, this field should contain the prompt text.")]
    public List<Localization>? Prompt { get; set; } = [];
}

public enum GuiActionPriority
{
    Primary = 1,
    Secondary = 2,
    Tertiary = 3
}

public sealed class Attachment
{
    [GraphQLDescription("The unique identifier for the attachment in UUIDv7 format.")]
    public Guid Id { get; set; }

    [GraphQLDescription("The display name of the attachment that should be used in GUIs.")]
    public List<Localization> DisplayName { get; set; } = [];

    [GraphQLDescription("The URLs associated with the attachment, each referring to a different representation of the attachment.")]
    public List<AttachmentUrl> Urls { get; set; } = [];

    [GraphQLDescription("The UTC timestamp when the attachment expires and is no longer available.")]
    public DateTimeOffset? ExpiresAt { get; set; }
}

public sealed class AttachmentUrl
{
    [GraphQLDescription("The unique identifier for the attachment URL in UUIDv7 format.")]
    public Guid Id { get; set; }

    [GraphQLDescription("The fully qualified URL of the attachment.")]
    public Uri Url { get; set; } = null!;

    [GraphQLDescription("The media type of the attachment.")]
    public string? MediaType { get; set; }

    [GraphQLDescription("What type of consumer the URL is intended for.")]
    public AttachmentUrlConsumer ConsumerType { get; set; }
}

public enum AttachmentUrlConsumer
{
    Gui = 1,
    Api = 2
}

public sealed class DialogEventPayload
{
    public Guid Id { get; set; }
    public DialogEventType Type { get; set; }
}

public enum DialogEventType
{
    DialogUpdated = 1,
    DialogDeleted = 2
}
