﻿using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Http;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;

public sealed class GetDialogDto
{
    /// <summary>
    /// The unique identifier for the dialog in UUIDv7 format.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid Id { get; set; }

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
    /// The party code representing the organization or person that the dialog belongs to in URN format
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
    /// The dialog unstructured text content
    /// </summary>
    public GetDialogContentDto Content { get; set; } = null!;

    /// <summary>
    /// The dialog token. May be used (if supported) against external URLs referred to in this dialog's apiActions,
    /// transmissions or attachments. Should also be used for front-channel embeds.
    /// </summary>
    public string? DialogToken { get; set; }

    /// <summary>
    /// The attachments associated with the dialog (on an aggregate level)
    /// </summary>
    public List<GetDialogDialogAttachmentDto> Attachments { get; set; } = [];

    /// <summary>
    /// The immutable list of transmissions associated with the dialog
    /// </summary>
    public List<GetDialogDialogTransmissionDto> Transmissions { get; set; } = [];

    /// <summary>
    /// The GUI actions associated with the dialog. Should be used in browser-based interactive frontends.
    /// </summary>
    public List<GetDialogDialogGuiActionDto> GuiActions { get; set; } = [];

    /// <summary>
    /// The API actions associated with the dialog. Should be used in specialized, non-browser-based integrations.
    /// </summary>
    public List<GetDialogDialogApiActionDto> ApiActions { get; set; } = [];

    /// <summary>
    /// An immutable list of activities associated with the dialog.
    /// </summary>
    public List<GetDialogDialogActivityDto> Activities { get; set; } = [];

    /// <summary>
    /// The list of seen log entries for the dialog newer than the dialog ChangedAt date.
    /// </summary>
    public List<GetDialogDialogSeenLogDto> SeenSinceLastUpdate { get; set; } = [];
}

public sealed class GetDialogDialogTransmissionDto
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
    /// Flag indicating if the authenticated user is authorized for this transmission. If not, embedded content and
    /// the attachments will not be available
    /// </summary>
    public bool IsAuthorized { get; set; }

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
    public GetDialogDialogTransmissionSenderActorDto Sender { get; set; } = null!;

    /// <summary>
    /// The transmission unstructured text content
    /// </summary>
    public GetDialogDialogTransmissionContentDto Content { get; set; } = null!;

    /// <summary>
    /// The transmission-level attachments
    /// </summary>
    public List<GetDialogTransmissionAttachmentDto> Attachments { get; set; } = [];
}

public sealed class GetDialogDialogSeenLogDto
{
    /// <summary>
    /// The unique identifier for the seen log entry in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The timestamp when the dialog revision was seen
    /// </summary>
    public DateTimeOffset SeenAt { get; set; }

    /// <summary>
    /// The actor that saw the dialog revision
    /// </summary>
    public GetDialogDialogSeenLogSeenByActorDto SeenBy { get; set; } = null!;

    /// <summary>
    /// Flag indicating whether the seen log entry was created via the service owner.
    ///
    /// This is used when the service owner uses the service owner API to implement its own frontend.
    /// </summary>
    public bool? IsViaServiceOwner { get; set; }

    /// <summary>
    /// Flag indicating whether the seen log entry was created by the current end user.
    /// </summary>
    public bool IsCurrentEndUser { get; set; }
}

public sealed class GetDialogDialogSeenLogSeenByActorDto
{
    /// <summary>
    /// The natural name of the person/business that saw the dialog revision.
    /// </summary>
    public string ActorName { get; set; } = null!;

    /// <summary>
    /// The identifier of the person/business that saw the dialog revision.
    /// </summary>
    /// <example>
    /// urn:altinn:person:identifier-no:01125512345
    /// urn:altinn:organization:identifier-no:912345678
    /// </example>
    public string ActorId { get; set; } = null!;
}

public sealed class GetDialogDialogTransmissionSenderActorDto
{
    /// <summary>
    /// The type of actor that sent the transmission.
    /// </summary>
    public ActorType.Values ActorType { get; set; }

    /// <summary>
    /// The name of the person or organization that sent the transmission.
    /// </summary>
    /// <example>Ola Nordmann</example>
    public string ActorName { get; set; } = null!;

    /// <summary>
    /// The identifier of the person or organization that sent the transmission.
    /// </summary>
    /// <example>urn:altinn:person:identifier-no:12018212345</example>
    public string ActorId { get; set; } = null!;
}

public sealed class GetDialogContentDto
{
    /// <summary>
    /// The title of the dialog. Always text/plain.
    /// </summary>
    public ContentValueDto Title { get; set; } = null!;

    /// <summary>
    /// A short summary of the dialog and its current state. Always text/plain.
    /// </summary>
    public ContentValueDto Summary { get; set; } = null!;

    /// <summary>
    /// Overridden sender name. If not supplied, assume "org" as the sender name. Always text/plain.
    /// </summary>
    public ContentValueDto? SenderName { get; set; }

    /// <summary>
    /// Additional information about the dialog, this may contain Markdown.
    /// </summary>
    public ContentValueDto? AdditionalInfo { get; set; }

    /// <summary>
    /// Used as the human-readable label used to describe the "ExtendedStatus" field. Always text/plain.
    /// </summary>
    public ContentValueDto? ExtendedStatus { get; set; }

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL.
    /// </summary>
    public ContentValueDto? MainContentReference { get; set; }
}

public sealed class GetDialogDialogTransmissionContentDto
{
    /// <summary>
    /// The transmission title. Always text/plain.
    /// </summary>
    public ContentValueDto Title { get; set; } = null!;

    /// <summary>
    /// The transmission summary. Always text/plain.
    /// </summary>
    public ContentValueDto Summary { get; set; } = null!;
}

public sealed class GetDialogDialogActivityDto
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
    /// The related activity identifier, if applicable. Must be present in the current dialog.
    /// </summary>
    public Guid? RelatedActivityId { get; set; }

    /// <summary>
    /// If the activity is related to a particular transmission, this field will contain the transmission identifier.
    /// </summary>
    public Guid? TransmissionId { get; set; }

    /// <summary>
    /// The actor that performed the activity.
    /// </summary>
    public GetDialogDialogActivityPerformedByActorDto PerformedBy { get; set; } = null!;

    /// <summary>
    /// Unstructured text describing the activity. Only set if the activity type is "Information".
    /// </summary>
    public List<LocalizationDto> Description { get; set; } = [];
}

public sealed class GetDialogDialogActivityPerformedByActorDto
{
    /// <summary>
    /// What type of actor performed the activity.
    /// </summary>
    public ActorType.Values ActorType { get; set; }

    /// <summary>
    /// The name of the person or organization that performed the activity.
    /// Only set if the actor type is "PartyRepresentative".
    /// </summary>
    public string? ActorName { get; set; }

    /// <summary>
    /// The identifier of the person or organization that performed the activity.
    /// May be omitted if ActorType is "ServiceOwner".
    /// </summary>
    public string? ActorId { get; set; }
}

public sealed class GetDialogDialogApiActionDto
{
    /// <summary>
    /// The unique identifier for the action in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// String identifier for the action, corresponding to the "action" attributeId used in the XACML service policy,
    /// which by default is the policy belonging to the service referred to by "serviceResource" in the dialog
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
    /// True if the authenticated user is authorized for this action. If not, the action will not be available
    /// and all endpoints will be replaced with a fixed placeholder.
    /// </summary>
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// The endpoints associated with the action.
    /// </summary>
    public List<GetDialogDialogApiActionEndpointDto> Endpoints { get; set; } = [];
}

public sealed class GetDialogDialogApiActionEndpointDto
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
    /// The fully qualified HTTPS URL of the API endpoint. Will be set to "urn:dialogporten:unauthorized" if the user is
    /// not authorized to perform the action.
    /// </summary>
    /// <example>
    /// https://someendpoint.com/api/v1/someaction
    /// urn:dialogporten:unauthorized
    /// </example>
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

public sealed class GetDialogDialogGuiActionDto
{
    /// <summary>
    /// The unique identifier for the action in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The action identifier for the action, corresponding to the "action" attributeId used in the XACML service policy,
    /// </summary>
    public string Action { get; set; } = null!;

    /// <summary>
    /// The fully qualified HTTPS URL of the action, to which the user will be redirected when the action is triggered. Will be set to
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
    /// Whether the user is authorized to perform the action.
    /// </summary>
    public bool IsAuthorized { get; set; }

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
    /// The title of the action, this should be short and in verb form. Always text/plain.
    /// </summary>
    public List<LocalizationDto> Title { get; set; } = [];

    /// <summary>
    /// If there should be a prompt asking the user for confirmation before the action is executed,
    /// this field should contain the prompt text.
    /// </summary>
    public List<LocalizationDto>? Prompt { get; set; } = [];
}

public sealed class GetDialogDialogAttachmentDto
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
    public List<GetDialogDialogAttachmentUrlDto> Urls { get; set; } = [];
}

public sealed class GetDialogDialogAttachmentUrlDto
{
    /// <summary>
    /// The unique identifier for the attachment URL in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The fully qualified HTTPS URL of the attachment.
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

public sealed class GetDialogTransmissionAttachmentDto
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
    public List<GetDialogTransmissionAttachmentUrlDto> Urls { get; set; } = [];
}

public sealed class GetDialogTransmissionAttachmentUrlDto
{
    /// <summary>
    /// The fully qualified HTTPS URL of the attachment. Will be set to "urn:dialogporten:unauthorized" if the user is
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
