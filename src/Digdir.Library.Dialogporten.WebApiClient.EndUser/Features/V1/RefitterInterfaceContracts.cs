using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1;

public class ServiceResourceMetadataList
{

    [JsonPropertyName("items")]
    public ICollection<ServiceResourceMetadata>? Items { get; set; } = null;

}

public class ServiceResourceMetadata
{

    [JsonPropertyName("serviceResource")]
    public ServiceResource ServiceResource { get; set; } = null!;

    [JsonPropertyName("roles")]
    public ICollection<ServiceResourceRole>? Roles { get; set; } = null!;

    [JsonPropertyName("accessPackages")]
    public ICollection<ServiceResourceAccessPackage>? AccessPackages { get; set; } = null!;

    [JsonPropertyName("serviceOwner")]
    public ServiceResourceOwner ServiceOwner { get; set; } = null!;

}

public class ServiceResource
{

    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = null!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("isDelegable")]
    public bool IsDelegable { get; set; } = false;

    [JsonPropertyName("minimumAuthenticationLevel")]
    public int MinimumAuthenticationLevel { get; set; } = 0;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = null!;

    [JsonPropertyName("links")]
    public Links Links { get; set; } = null!;

}

public class Localization
{

    /// <summary>
    /// The localized text (or URL if a front-channel embed).
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;

    /// <summary>
    /// The language code of the localization in ISO 639-1 format.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public string LanguageCode { get; set; } = null!;

}

public class Links
{

    [JsonPropertyName("metadata")]
    public string Metadata { get; set; } = null!;

}

public class ServiceResourceRole
{

    [JsonPropertyName("urn")]
    public string Urn { get; set; } = null!;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = null!;

    [JsonPropertyName("links")]
    public Links Links { get; set; } = null!;

}

public class ServiceResourceAccessPackage
{

    [JsonPropertyName("urn")]
    public string Urn { get; set; } = null!;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = null!;

    [JsonPropertyName("links")]
    public Links Links { get; set; } = null!;

}

public class ServiceResourceOwner
{

    [JsonPropertyName("orgNumber")]
    public string OrgNumber { get; set; } = null!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = null!;

}

public partial class AcceptedLanguages
{

    [JsonPropertyName("acceptedLanguage")]
    public ICollection<AcceptedLanguage>? AcceptedLanguage { get; set; } = null!;

}

public partial class AcceptedLanguage
{

    [JsonPropertyName("languageCode")]
    public string LanguageCode { get; set; } = null!;

    [JsonPropertyName("weight")]
    public int Weight { get; set; } = 0;

}

public class Limits
{

    [JsonPropertyName("endUserSearch")]
    public EndUserSearchLimits EndUserSearch { get; set; } = null!;

    [JsonPropertyName("serviceOwnerSearch")]
    public ServiceOwnerSearchLimits ServiceOwnerSearch { get; set; } = null!;

}

public class EndUserSearchLimits
{

    [JsonPropertyName("maxPartyFilterValues")]
    public int MaxPartyFilterValues { get; set; } = 0;

    [JsonPropertyName("maxServiceResourceFilterValues")]
    public int MaxServiceResourceFilterValues { get; set; } = 0;

    [JsonPropertyName("maxOrgFilterValues")]
    public int MaxOrgFilterValues { get; set; } = 0;

    [JsonPropertyName("maxExtendedStatusFilterValues")]
    public int MaxExtendedStatusFilterValues { get; set; } = 0;

}

public class ServiceOwnerSearchLimits
{

    [JsonPropertyName("maxPartyFilterValues")]
    public int MaxPartyFilterValues { get; set; } = 0;

    [JsonPropertyName("maxServiceResourceFilterValues")]
    public int MaxServiceResourceFilterValues { get; set; } = 0;

    [JsonPropertyName("maxExtendedStatusFilterValues")]
    public int MaxExtendedStatusFilterValues { get; set; } = 0;

}

public class AuthorizedServiceResourceList
{

    [JsonPropertyName("items")]
    public ICollection<ServiceResourceMetadata>? Items { get; set; } = null!;

}

public class LabelAssignmentLog
{

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("action")]
    public string Action { get; set; } = null!;

    [JsonPropertyName("performedBy")]
    public Actor PerformedBy { get; set; } = null!;

}

public class Actor
{

    /// <summary>
    /// The type of actor; either the service owner, or someone representing the party.
    /// </summary>
    [JsonPropertyName("actorType")]
    [JsonConverter(typeof(JsonStringEnumConverter<ActorType>))]
    public ActorType ActorType { get; set; } = default!;

    /// <summary>
    /// The name of the actor.
    /// </summary>
    [JsonPropertyName("actorName")]
    public string? ActorName { get; set; } = null!;

    /// <summary>
    /// The identifier (national identity number or organization number) of the actor.
    /// </summary>
    [JsonPropertyName("actorId")]
    public string? ActorId { get; set; } = null!;

}

public enum ActorType
{

    [System.Runtime.Serialization.EnumMember(Value = @"PartyRepresentative")]
    PartyRepresentative = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"ServiceOwner")]
    ServiceOwner = 1,

}

public class ProblemDetails
{

    [JsonPropertyName("type")]
    public string? Type { get; set; } = null!;

    [JsonPropertyName("title")]
    public string? Title { get; set; } = null!;

    [JsonPropertyName("status")]
    public int? Status { get; set; } = null!;

    [JsonPropertyName("detail")]
    public string? Detail { get; set; } = null!;

    [JsonPropertyName("instance")]
    public string? Instance { get; set; } = null!;

    [JsonPropertyName("statusDescription")]
    public string? StatusDescription { get; set; } = null!;

    [JsonPropertyName("code")]
    public string? Code { get; set; } = null!;

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; } = null!;

    [JsonPropertyName("validationErrors")]
    public ICollection<ProblemDetailsError>? ValidationErrors { get; set; } = null!;

    [JsonPropertyName("errors")]
    public IDictionary<string, ICollection<string>> Errors { get; set; } = null!;

    private IDictionary<string, object>? _additionalProperties;

    [JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
        set { _additionalProperties = value; }
    }

}

public class ProblemDetailsError
{

    [JsonPropertyName("title")]
    public string? Title { get; set; } = null!;

    [JsonPropertyName("code")]
    public string? Code { get; set; } = null!;

    [JsonPropertyName("detail")]
    public string? Detail { get; set; } = null!;

    [JsonPropertyName("paths")]
    public ICollection<string>? Paths { get; set; } = null!;

    private IDictionary<string, object>? _additionalProperties;

    [JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get => _additionalProperties ??= new Dictionary<string, object>();
        set => _additionalProperties = value;
    }

}

public class SetDialogSystemLabelRequest
{

    /// <summary>
    /// List of system labels to set on target dialogs
    /// </summary>
    [JsonPropertyName("systemLabels")]
    [Obsolete("Use AddLabels instead. This property will be removed in a future version.")]
    public ICollection<SystemLabel>? SystemLabels { get; set; } = null!;

    /// <summary>
    /// List of system labels to add to the target dialog. If multiple instances of 'bin', 'archive', or 'default' are provided, the last one will be used.
    /// </summary>
    [JsonPropertyName("addLabels")]
    public ICollection<SystemLabel>? AddLabels { get; set; } = null!;

    /// <summary>
    /// List of system labels to remove from the target dialog. If 'bin' or 'archive' is removed, the 'default' label will be added automatically unless 'bin' or 'archive' is also in the AddLabels list.
    /// </summary>
    [JsonPropertyName("removeLabels")]
    public ICollection<SystemLabel>? RemoveLabels { get; set; } = null!;

}

public enum SystemLabel
{

    [System.Runtime.Serialization.EnumMember(Value = @"Default")]
    Default = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Bin")]
    Bin = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"Archive")]
    Archive = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"MarkedAsUnopened")]
    MarkedAsUnopened = 3,

    [System.Runtime.Serialization.EnumMember(Value = @"Sent")]
    Sent = 4,

}

public class DialogTransmissionSearchItem
{

    /// <summary>
    /// The unique identifier for the transmission in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The date and time when the transmission was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = default!;

    /// <summary>
    /// The authorization attribute associated with the transmission.
    /// </summary>
    [JsonPropertyName("authorizationAttribute")]
    public string? AuthorizationAttribute { get; set; } = null!;

    /// <summary>
    /// Flag indicating if the authenticated user is authorized for this transmission. If not, embedded content and
    /// <br/>the attachments will not be available.
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool IsAuthorized { get; set; } = false;

    /// <summary>
    /// The extended type URI for the transmission.
    /// </summary>
    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; } = null!;

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// </summary>
    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; } = null!;

    /// <summary>
    /// The unique identifier for the related transmission, if any.
    /// </summary>
    [JsonPropertyName("relatedTransmissionId")]
    public Guid? RelatedTransmissionId { get; set; } = null!;

    /// <summary>
    /// The date and time when the transmission was deleted, if applicable.
    /// </summary>
    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; set; } = null!;

    /// <summary>
    /// The type of the transmission.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogTransmissionType>))]
    public DialogTransmissionType Type { get; set; } = default!;

    /// <summary>
    /// The sender actor information for the transmission.
    /// </summary>
    [JsonPropertyName("sender")]
    public Actor Sender { get; set; } = null!;

    /// <summary>
    /// The content of the transmission.
    /// </summary>
    [JsonPropertyName("content")]
    public DialogTransmissionSearchContent Content { get; set; } = null!;

    /// <summary>
    /// The attachments associated with the transmission.
    /// </summary>
    [JsonPropertyName("attachments")]
    public ICollection<DialogTransmissionSearchAttachment>? Attachments { get; set; } = null!;

    /// <summary>
    /// The navigational actions associated with the transmission.
    /// </summary>
    [JsonPropertyName("navigationalActions")]
    public ICollection<DialogTransmissionSearchNavigationalAction>? NavigationalActions { get; set; } = null!;

}

public class BulkSetSystemLabel
{

    /// <summary>
    /// List of target dialog ids with optional revision ids
    /// </summary>
    [JsonPropertyName("dialogs")]
    public ICollection<DialogRevision>? Dialogs { get; set; } = null!;

    /// <summary>
    /// List of system labels to set on target dialogs
    /// </summary>
    [JsonPropertyName("systemLabels")]
    [Obsolete("Use AddLabels instead. This property will be removed in a future version.")]
    public ICollection<SystemLabel>? SystemLabels { get; set; } = null!;

    /// <summary>
    /// List of system labels to add to the target dialogs. If multiple instances of 'bin', 'archive', or 'default' are provided, the last one will be used.
    /// </summary>
    [JsonPropertyName("addLabels")]
    public ICollection<SystemLabel>? AddLabels { get; set; } = null!;

    /// <summary>
    /// List of system labels to remove from the target dialogs. If 'bin' or 'archive' is removed, the 'default' label will be added automatically unless 'bin' or 'archive' is also in the AddLabels list.
    /// </summary>
    [JsonPropertyName("removeLabels")]
    public ICollection<SystemLabel>? RemoveLabels { get; set; } = null!;

}

public class DialogRevision
{

    /// <summary>
    /// Target dialog id for system labels
    /// </summary>
    [JsonPropertyName("dialogId")]
    public Guid DialogId { get; set; } = Guid.Empty;

    /// <summary>
    /// Optional end user context revision to match against. If supplied and not matching current revision, the entire operation will fail.
    /// </summary>
    [JsonPropertyName("endUserContextRevision")]
    public Guid? EndUserContextRevision { get; set; } = null!;

}

public enum DialogTransmissionType
{

    [System.Runtime.Serialization.EnumMember(Value = @"Information")]
    Information = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Acceptance")]
    Acceptance = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"Rejection")]
    Rejection = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"Request")]
    Request = 3,

    [System.Runtime.Serialization.EnumMember(Value = @"Alert")]
    Alert = 4,

    [System.Runtime.Serialization.EnumMember(Value = @"Decision")]
    Decision = 5,

    [System.Runtime.Serialization.EnumMember(Value = @"Submission")]
    Submission = 6,

    [System.Runtime.Serialization.EnumMember(Value = @"Correction")]
    Correction = 7,

}

public class DialogTransmissionSearchContent
{

    /// <summary>
    /// The title of the content.
    /// </summary>
    [JsonPropertyName("title")]
    public ContentValue Title { get; set; } = null!;

    /// <summary>
    /// The summary of the content.
    /// </summary>
    [JsonPropertyName("summary")]
    public ContentValue? Summary { get; set; } = null!;

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL.
    /// </summary>
    [JsonPropertyName("contentReference")]
    public ContentValue? ContentReference { get; set; } = null!;

}

public class ContentValue
{

    /// <summary>
    /// A list of localizations for the content.
    /// </summary>
    [JsonPropertyName("value")]
    public ICollection<Localization>? Value { get; set; } = null!;

    /// <summary>
    /// Media type of the content, this can also indicate that the content is embeddable.
    /// </summary>
    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = null!;

    /// <summary>
    /// True if the authenticated user is authorized for this content. If not, the endpoints will
    /// <br/>be replaced with a fixed placeholder. Can be null if not applicable.
    /// <br/>            
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool? IsAuthorized { get; set; } = null!;

}

public class DialogTransmissionSearchAttachment
{

    /// <summary>
    /// The unique identifier for the attachment in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The display name of the attachment that should be used in GUIs.
    /// </summary>
    [JsonPropertyName("displayName")]
    public ICollection<Localization>? DisplayName { get; set; } = null!;

    /// <summary>
    /// The logical name of the attachment.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; } = null!;

    /// <summary>
    /// The URLs associated with the attachment, each referring to a different representation of the attachment.
    /// </summary>
    [JsonPropertyName("urls")]
    public ICollection<DialogTransmissionSearchAttachmentUrl>? Urls { get; set; } = null!;

    /// <summary>
    /// The UTC timestamp when the attachment expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; } = null!;

}

public class DialogTransmissionSearchAttachmentUrl
{

    /// <summary>
    /// The unique identifier for the attachment URL in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The fully qualified URL of the attachment. Will be set to "urn:dialogporten:unauthorized" if the user is
    /// <br/>not authorized to access the transmission.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The media type of the attachment.
    /// </summary>
    [JsonPropertyName("mediaType")]
    public string? MediaType { get; set; } = null!;

    /// <summary>
    /// The type of consumer the URL is intended for.
    /// </summary>
    [JsonPropertyName("consumerType")]
    [JsonConverter(typeof(JsonStringEnumConverter<AttachmentUrlConsumerType>))]
    public AttachmentUrlConsumerType ConsumerType { get; set; } = default!;

}

public enum AttachmentUrlConsumerType
{

    [System.Runtime.Serialization.EnumMember(Value = @"Gui")]
    Gui = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Api")]
    Api = 1,

}

public class DialogTransmissionSearchNavigationalAction
{

    /// <summary>
    /// The title of the navigational action.
    /// </summary>
    [JsonPropertyName("title")]
    public ICollection<Localization>? Title { get; set; } = null!;

    /// <summary>
    /// The fully qualified URL of the navigational action. Will be set to \"urn:dialogporten:unauthorized\" if the user is
    /// <br/>not authorized to access the transmission, or \"urn:dialogporten:expired\" if the action has expired.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The UTC timestamp when the navigational action expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; } = null!;

}

public class DialogSeenLogSearchItem
{

    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    [JsonPropertyName("seenAt")]
    public DateTimeOffset SeenAt { get; set; } = default!;

    [JsonPropertyName("seenBy")]
    public Actor SeenBy { get; set; } = null!;

    [JsonPropertyName("isViaServiceOwner")]
    public bool IsViaServiceOwner { get; set; } = false;

    [JsonPropertyName("isCurrentEndUser")]
    public bool IsCurrentEndUser { get; set; } = false;

}

public class DialogActivitySearchItem
{

    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = default!;

    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; } = null!;

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogActivityType>))]
    public DialogActivityType Type { get; set; } = default!;

    [JsonPropertyName("transmissionId")]
    public Guid? TransmissionId { get; set; } = null!;

    [JsonPropertyName("description")]
    public ICollection<Localization>? Description { get; set; } = null!;

}

public enum DialogActivityType
{

    [System.Runtime.Serialization.EnumMember(Value = @"DialogCreated")]
    DialogCreated = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"DialogClosed")]
    DialogClosed = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"Information")]
    Information = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"TransmissionOpened")]
    TransmissionOpened = 3,

    [System.Runtime.Serialization.EnumMember(Value = @"PaymentMade")]
    PaymentMade = 4,

    [System.Runtime.Serialization.EnumMember(Value = @"SignatureProvided")]
    SignatureProvided = 5,

    [System.Runtime.Serialization.EnumMember(Value = @"DialogOpened")]
    DialogOpened = 6,

    [System.Runtime.Serialization.EnumMember(Value = @"DialogDeleted")]
    DialogDeleted = 7,

    [System.Runtime.Serialization.EnumMember(Value = @"DialogRestored")]
    DialogRestored = 8,

    [System.Runtime.Serialization.EnumMember(Value = @"SentToSigning")]
    SentToSigning = 9,

    [System.Runtime.Serialization.EnumMember(Value = @"SentToFormFill")]
    SentToFormFill = 10,

    [System.Runtime.Serialization.EnumMember(Value = @"SentToSendIn")]
    SentToSendIn = 11,

    [System.Runtime.Serialization.EnumMember(Value = @"SentToPayment")]
    SentToPayment = 12,

    [System.Runtime.Serialization.EnumMember(Value = @"FormSubmitted")]
    FormSubmitted = 13,

    [System.Runtime.Serialization.EnumMember(Value = @"FormSaved")]
    FormSaved = 14,

    [System.Runtime.Serialization.EnumMember(Value = @"CorrespondenceOpened")]
    CorrespondenceOpened = 15,

    [System.Runtime.Serialization.EnumMember(Value = @"CorrespondenceConfirmed")]
    CorrespondenceConfirmed = 16,

}

public class PaginatedListOfDialogListItem
{

    /// <summary>
    /// The paginated list of items
    /// </summary>
    [JsonPropertyName("items")]
    public ICollection<DialogListItem> Items { get; set; } = [];

    /// <summary>
    /// Whether there are more items available that can be fetched by supplying the continuation token
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; } = false;

    /// <summary>
    /// The continuation token to be used to fetch the next page of items
    /// </summary>
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; } = null!;

    /// <summary>
    /// The current sorting order of the items
    /// </summary>
    [JsonPropertyName("orderBy")]
    public string OrderBy { get; set; } = null!;

}

public class DialogListItem
{

    /// <summary>
    /// The unique identifier for the dialog in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The service owner code representing the organization (service owner) related to this dialog.
    /// </summary>
    [JsonPropertyName("org")]
    public string Org { get; set; } = null!;

    /// <summary>
    /// The service identifier for the service that the dialog is related to in URN-format.
    /// <br/>This corresponds to a service resource in the Altinn Resource Registry.
    /// </summary>
    [JsonPropertyName("serviceResource")]
    public string ServiceResource { get; set; } = null!;

    /// <summary>
    /// The ServiceResource type, as defined in Altinn Resource Registry (see ResourceType).
    /// </summary>
    [JsonPropertyName("serviceResourceType")]
    public string ServiceResourceType { get; set; } = null!;

    /// <summary>
    /// The party code representing the organization or person that the dialog belongs to in URN format.
    /// </summary>
    [JsonPropertyName("party")]
    public string Party { get; set; } = null!;

    /// <summary>
    /// Advisory indicator of progress, represented as 1-100 percentage value. 100% representing a dialog that has come
    /// <br/>to a natural completion (successful or not).
    /// </summary>
    [JsonPropertyName("progress")]
    public int? Progress { get; set; } = null!;

    /// <summary>
    /// Optional process identifier used to indicate a business process this dialog belongs to.
    /// </summary>
    [JsonPropertyName("process")]
    public string? Process { get; set; } = null!;

    /// <summary>
    /// Optional preceding process identifier to indicate the business process that preceded the process indicated in the "Process" field. Cannot be set without also "Process" being set.
    /// </summary>
    [JsonPropertyName("precedingProcess")]
    public string? PrecedingProcess { get; set; } = null!;

    /// <summary>
    /// The number of attachments in the dialog made available for browser-based frontends.
    /// </summary>
    [JsonPropertyName("guiAttachmentCount")]
    public int? GuiAttachmentCount { get; set; } = null!;

    /// <summary>
    /// Arbitrary string with a service-specific indicator of status, typically used to indicate a fine-grained state of
    /// <br/>the dialog to further specify the "status" enum.
    /// <br/>            
    /// <br/>Refer to the service-specific documentation provided by the service owner for details on the possible values (if
    /// <br/>in use).
    /// </summary>
    [JsonPropertyName("extendedStatus")]
    public string? ExtendedStatus { get; set; } = null!;

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// <br/>            
    /// <br/>Refer to the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; } = null!;

    /// <summary>
    /// The date and time when the dialog was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = default!;

    /// <summary>
    /// The date and time when the dialog was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; } = default!;

    /// <summary>
    /// The date and time when the dialog content was last updated.
    /// </summary>
    [JsonPropertyName("contentUpdatedAt")]
    public DateTimeOffset ContentUpdatedAt { get; set; } = default!;

    /// <summary>
    /// The due date for the dialog. This is the last date when the dialog is expected to be completed.
    /// </summary>
    [JsonPropertyName("dueAt")]
    public DateTimeOffset? DueAt { get; set; } = null!;

    /// <summary>
    /// The aggregated status of the dialog.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogStatus>))]
    public DialogStatus Status { get; set; } = default!;

    /// <summary>
    /// Whether the service owner has not yet reported all dialog Transmissions they sent as seen by the end user.
    /// <br/>A Transmission is considered "sent from the service owner" if the DialogTransmissionType is not one of Submission or Correction.
    /// <br/>            
    /// <br/>The value of this field is:
    /// <br/>- true when there are any new unopened Transmissions sent from the service owner.
    /// <br/>- false when the service owner has created an Activity of type TransmissionOpened for all Transmissions sent from the service owner. The Activities must each contain the relevant Id for all relevant Transmissions.
    /// <br/>            
    /// <br/>Note that the value is
    /// <br/>- determined by the service owner and not to be confused with IsContentSeen
    /// <br/>- not affected by SystemLabels
    /// <br/>            
    /// <br/>For correspondence: HasUnopenedContent is still true until the service owner also adds a Dialog level Activity (no transmission id) of type CorrespondenceOpened
    /// </summary>
    [JsonPropertyName("hasUnopenedContent")]
    public bool HasUnopenedContent { get; set; } = false;

    /// <summary>
    /// System defined label used to categorize dialogs.
    /// <br/>This is obsolete and will only show; Default, Bin or Archive.
    /// <br/>Use SystemLabels on EndUserContext instead.
    /// </summary>
    [JsonPropertyName("systemLabel")]
    [JsonConverter(typeof(JsonStringEnumConverter<SystemLabel>))]
    [Obsolete("Use EndUserContext.SystemLabels instead.")]
    public SystemLabel SystemLabel { get; set; } = default!;

    /// <summary>
    /// Indicates if this dialog is intended for API consumption only and should not be shown in frontends aimed at humans.
    /// <br/>When true, human-readable content like title and summary are not required.
    /// </summary>
    [JsonPropertyName("isApiOnly")]
    public bool IsApiOnly { get; set; } = false;

    /// <summary>
    /// The number of transmissions sent by the service owner
    /// </summary>
    [JsonPropertyName("fromServiceOwnerTransmissionsCount")]
    public int FromServiceOwnerTransmissionsCount { get; set; } = 0;

    /// <summary>
    /// The number of transmissions sent by a party representative
    /// </summary>
    [JsonPropertyName("fromPartyTransmissionsCount")]
    public int FromPartyTransmissionsCount { get; set; } = 0;

    /// <summary>
    /// The latest entry in the dialog's activity log.
    /// </summary>
    [JsonPropertyName("latestActivity")]
    public DialogActivityListItem? LatestActivity { get; set; } = null!;

    /// <summary>
    /// The list of seen log entries for the dialog newer than the dialog UpdatedAt date.
    /// </summary>
    [JsonPropertyName("seenSinceLastUpdate")]
    public ICollection<DialogSeenLogListItem>? SeenSinceLastUpdate { get; set; } = null!;

    /// <summary>
    /// The list of seen log entries for the dialog newer than the dialog ContentUpdatedAt date.
    /// </summary>
    [JsonPropertyName("seenSinceLastContentUpdate")]
    public ICollection<DialogSeenLogListItem>? SeenSinceLastContentUpdate { get; set; } = null!;

    /// <summary>
    /// Indicates whether a dialog has been seen since its last content update.
    /// <br/>            
    /// <br/>The value of this field is
    /// <br/>- true if the dialog has been retrieved since its last content update by either GET /enduser/dialogs/{dialogId} or GET /serviceowner/dialogs/{dialogId}?EndUserId={userId} and there is no SystemLabels MarkedAsUnopened
    /// <br/>- false if there is a SystemLabels MarkedAsUnopened, even if the dialog has been seen since its last content update
    /// <br/>- false after the dialog receives a content update.
    /// <br/>            
    /// <br/>Note that the value is determined by Dialogporten and not to be confused with HasUnopenedContent
    /// </summary>
    [JsonPropertyName("isContentSeen")]
    public bool IsContentSeen { get; set; } = false;

    /// <summary>
    /// Metadata about the dialog owned by end-users.
    /// </summary>
    [JsonPropertyName("endUserContext")]
    public DialogEndUserContextListItem EndUserContext { get; set; } = null!;

    /// <summary>
    /// The content of the dialog in search results. May be null for API-only dialogs, which are not required to have content.
    /// </summary>
    [JsonPropertyName("content")]
    public DialogContentSummary? Content { get; set; } = null!;

}

public enum DialogStatus
{

    [System.Runtime.Serialization.EnumMember(Value = @"InProgress")]
    InProgress = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Draft")]
    Draft = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"RequiresAttention")]
    RequiresAttention = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"Completed")]
    Completed = 3,

    [System.Runtime.Serialization.EnumMember(Value = @"NotApplicable")]
    NotApplicable = 4,

    [System.Runtime.Serialization.EnumMember(Value = @"Awaiting")]
    Awaiting = 5,

}

public class DialogActivityListItem
{

    /// <summary>
    /// The unique identifier for the activity in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The date and time when the activity was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; } = null!;

    /// <summary>
    /// An arbitrary string with a service-specific activity type.
    /// <br/>            
    /// <br/>Consult the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; } = null!;

    /// <summary>
    /// The type of activity.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogActivityType>))]
    public DialogActivityType Type { get; set; } = default!;

    /// <summary>
    /// If the activity is related to a particular transmission, this field will contain the transmission identifier.
    /// </summary>
    [JsonPropertyName("transmissionId")]
    public Guid? TransmissionId { get; set; } = null!;

    /// <summary>
    /// The actor that performed the activity.
    /// </summary>
    [JsonPropertyName("performedBy")]
    public Actor PerformedBy { get; set; } = null!;

    /// <summary>
    /// Unstructured text describing the activity. Only set if the activity type is "Information".
    /// </summary>
    [JsonPropertyName("description")]
    public ICollection<Localization>? Description { get; set; } = null!;

}

public class DialogSeenLogListItem
{

    /// <summary>
    /// The unique identifier for the seen log entry in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The timestamp when the dialog revision was seen.
    /// </summary>
    [JsonPropertyName("seenAt")]
    public DateTimeOffset SeenAt { get; set; } = default!;

    /// <summary>
    /// The actor that saw the dialog revision.
    /// </summary>
    [JsonPropertyName("seenBy")]
    public Actor SeenBy { get; set; } = null!;

    /// <summary>
    /// Flag indicating whether the seen log entry was created via the service owner.
    /// <br/>            
    /// <br/>This is used when the service owner uses the service owner API to implement its own frontend.
    /// </summary>
    [JsonPropertyName("isViaServiceOwner")]
    public bool? IsViaServiceOwner { get; set; } = null!;

    /// <summary>
    /// Flag indicating whether the seen log entry was created by the current end user.
    /// </summary>
    [JsonPropertyName("isCurrentEndUser")]
    public bool IsCurrentEndUser { get; set; } = false;

}

public class DialogEndUserContextListItem
{

    /// <summary>
    /// The unique identifier for the end user context revision in UUIDv4 format.
    /// </summary>
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; } = Guid.Empty;

    /// <summary>
    /// System defined labels used to categorize dialogs.
    /// </summary>
    [JsonPropertyName("systemLabels")]
    public ICollection<SystemLabel>? SystemLabels { get; set; } = null!;

}

public class DialogContentSummary
{

    /// <summary>
    /// The title of the dialog.
    /// </summary>
    [JsonPropertyName("title")]
    public ContentValue Title { get; set; } = null!;

    /// <summary>
    /// A short summary of the dialog and its current state.
    /// </summary>
    [JsonPropertyName("summary")]
    public ContentValue? Summary { get; set; } = null!;

    /// <summary>
    /// Overridden sender name. If not supplied, assume "org" as the sender name.
    /// </summary>
    [JsonPropertyName("senderName")]
    public ContentValue? SenderName { get; set; } = null!;

    /// <summary>
    /// Used as the human-readable label used to describe the "ExtendedStatus" field.
    /// </summary>
    [JsonPropertyName("extendedStatus")]
    public ContentValue? ExtendedStatus { get; set; } = null!;

}

public class DialogTransmissionDetails
{

    /// <summary>
    /// The unique identifier for the transmission in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The date and time when the transmission was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = default!;

    /// <summary>
    /// The authorization attribute associated with the transmission.
    /// </summary>
    [JsonPropertyName("authorizationAttribute")]
    public string? AuthorizationAttribute { get; set; } = null!;

    /// <summary>
    /// Flag indicating if the authenticated user is authorized for this transmission. If not, embedded content and
    /// <br/>the attachments will not be available.
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool IsAuthorized { get; set; } = false;

    /// <summary>
    /// The extended type URI for the transmission.
    /// </summary>
    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; } = null!;

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// </summary>
    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; } = null!;

    /// <summary>
    /// The unique identifier for the related transmission, if any.
    /// </summary>
    [JsonPropertyName("relatedTransmissionId")]
    public Guid? RelatedTransmissionId { get; set; } = null!;

    /// <summary>
    /// The date and time when the transmission was deleted, if applicable.
    /// </summary>
    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; set; } = null!;

    /// <summary>
    /// The type of the transmission.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogTransmissionType>))]
    public DialogTransmissionType Type { get; set; } = default!;

    /// <summary>
    /// The sender actor information for the transmission.
    /// </summary>
    [JsonPropertyName("sender")]
    public Actor Sender { get; set; } = null!;

    /// <summary>
    /// The content of the transmission.
    /// </summary>
    [JsonPropertyName("content")]
    public DialogTransmissionContentDetails Content { get; set; } = null!;

    /// <summary>
    /// The attachments associated with the transmission.
    /// </summary>
    [JsonPropertyName("attachments")]
    public ICollection<DialogTransmissionAttachmentDetails>? Attachments { get; set; } = null!;

    /// <summary>
    /// The navigational actions associated with the transmission.
    /// </summary>
    [JsonPropertyName("navigationalActions")]
    public ICollection<DialogTransmissionNavigationalActionDetails>? NavigationalActions { get; set; } = null!;

}

public class DialogTransmissionContentDetails
{

    /// <summary>
    /// The title of the content.
    /// </summary>
    [JsonPropertyName("title")]
    public ContentValue Title { get; set; } = null!;

    /// <summary>
    /// The summary of the content.
    /// </summary>
    [JsonPropertyName("summary")]
    public ContentValue? Summary { get; set; } = null!;

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL.
    /// </summary>
    [JsonPropertyName("contentReference")]
    public ContentValue? ContentReference { get; set; } = null!;

}

public class DialogTransmissionAttachmentDetails
{

    /// <summary>
    /// The unique identifier for the attachment in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The display name of the attachment that should be used in GUIs.
    /// </summary>
    [JsonPropertyName("displayName")]
    public ICollection<Localization>? DisplayName { get; set; } = null!;

    /// <summary>
    /// The logical name of the attachment.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; } = null!;

    /// <summary>
    /// The URLs associated with the attachment, each referring to a different representation of the attachment.
    /// </summary>
    [JsonPropertyName("urls")]
    public ICollection<DialogTransmissionAttachmentUrlDetails>? Urls { get; set; } = null!;

    /// <summary>
    /// The UTC timestamp when the attachment expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; } = null!;

}

public class DialogTransmissionAttachmentUrlDetails
{

    /// <summary>
    /// The unique identifier for the attachment URL in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The fully qualified URL of the attachment. Will be set to "urn:dialogporten:unauthorized" if the user is
    /// <br/>not authorized to access the transmission.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The media type of the attachment.
    /// </summary>
    [JsonPropertyName("mediaType")]
    public string? MediaType { get; set; } = null!;

    /// <summary>
    /// The type of consumer the URL is intended for.
    /// </summary>
    [JsonPropertyName("consumerType")]
    [JsonConverter(typeof(JsonStringEnumConverter<AttachmentUrlConsumerType>))]
    public AttachmentUrlConsumerType ConsumerType { get; set; } = default!;

}

public class DialogTransmissionNavigationalActionDetails
{

    /// <summary>
    /// The title of the navigational action.
    /// </summary>
    [JsonPropertyName("title")]
    public ICollection<Localization>? Title { get; set; } = null!;

    /// <summary>
    /// The fully qualified URL of the navigational action. Will be set to \"urn:dialogporten:unauthorized\" if the user is
    /// <br/>not authorized to access the transmission, or \"urn:dialogporten:expired\" if the action has expired.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The UTC timestamp when the navigational action expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; } = null!;

}

public class DialogSeenLogDetails
{

    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    [JsonPropertyName("seenAt")]
    public DateTimeOffset SeenAt { get; set; } = default!;

    [JsonPropertyName("seenBy")]
    public Actor SeenBy { get; set; } = null!;

    [JsonPropertyName("isViaServiceOwner")]
    public bool IsViaServiceOwner { get; set; } = false;

    [JsonPropertyName("isCurrentEndUser")]
    public bool IsCurrentEndUser { get; set; } = false;

}

public class DialogActivityDetails
{

    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; } = null!;

    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; } = null!;

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogActivityType>))]
    public DialogActivityType Type { get; set; } = default!;

    [JsonPropertyName("transmissionId")]
    public Guid? TransmissionId { get; set; } = null!;

    [JsonPropertyName("performedBy")]
    public Actor PerformedBy { get; set; } = null!;

    [JsonPropertyName("description")]
    public ICollection<Localization>? Description { get; set; } = null!;

}

public class Dialog
{

    /// <summary>
    /// The unique identifier for the dialog in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The unique identifier for the revision in UUIDv4 format.
    /// </summary>
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; } = Guid.Empty;

    /// <summary>
    /// The service owner code representing the organization (service owner) related to this dialog.
    /// </summary>
    [JsonPropertyName("org")]
    public string Org { get; set; } = null!;

    /// <summary>
    /// The service identifier for the service that the dialog is related to in URN-format.
    /// <br/>This corresponds to a service resource in the Altinn Resource Registry.
    /// </summary>
    [JsonPropertyName("serviceResource")]
    public string ServiceResource { get; set; } = null!;

    /// <summary>
    /// The ServiceResource type, as defined in Altinn Resource Registry (see ResourceType).
    /// </summary>
    [JsonPropertyName("serviceResourceType")]
    public string ServiceResourceType { get; set; } = null!;

    /// <summary>
    /// The party code representing the organization or person that the dialog belongs to in URN format.
    /// </summary>
    [JsonPropertyName("party")]
    public string Party { get; set; } = null!;

    /// <summary>
    /// Advisory indicator of progress, represented as 1-100 percentage value. 100% representing a dialog that has come
    /// <br/>to a natural completion (successful or not).
    /// </summary>
    [JsonPropertyName("progress")]
    public int? Progress { get; set; } = null!;

    /// <summary>
    /// Optional process identifier used to indicate a business process this dialog belongs to.
    /// </summary>
    [JsonPropertyName("process")]
    public string? Process { get; set; } = null!;

    /// <summary>
    /// Optional preceding process identifier to indicate the business process that preceded the process indicated in the "Process" field. Cannot be set without also "Process" being set.
    /// </summary>
    [JsonPropertyName("precedingProcess")]
    public string? PrecedingProcess { get; set; } = null!;

    /// <summary>
    /// Arbitrary string with a service-specific indicator of status, typically used to indicate a fine-grained state of
    /// <br/>the dialog to further specify the "status" enum.
    /// <br/>            
    /// <br/>Refer to the service-specific documentation provided by the service owner for details on the possible values (if
    /// <br/>in use).
    /// </summary>
    [JsonPropertyName("extendedStatus")]
    public string? ExtendedStatus { get; set; } = null!;

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// <br/>            
    /// <br/>Refer to the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; } = null!;

    /// <summary>
    /// The due date for the dialog. Dialogs past due date might be marked as such in frontends but will still be available.
    /// </summary>
    [JsonPropertyName("dueAt")]
    public DateTimeOffset? DueAt { get; set; } = null!;

    /// <summary>
    /// The expiration date for the dialog. This is the last date when the dialog is available for the end user.
    /// <br/>            
    /// <br/>After this date is passed, the dialog will be considered expired and no longer available for the end user in any
    /// <br/>API. If not supplied, the dialog will be considered to never expire. This field can be changed by the service
    /// <br/>owner after the dialog has been created.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; } = null!;

    /// <summary>
    /// The date and time when the dialog was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = default!;

    /// <summary>
    /// The date and time when the dialog was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; } = default!;

    /// <summary>
    /// The date and time when the dialog content was last updated.
    /// </summary>
    [JsonPropertyName("contentUpdatedAt")]
    public DateTimeOffset ContentUpdatedAt { get; set; } = default!;

    /// <summary>
    /// The aggregated status of the dialog.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogStatus>))]
    public DialogStatus Status { get; set; } = default!;

    /// <summary>
    /// System defined label used to categorize dialogs.
    /// <br/>This is obsolete and will only show; Default, Bin or Archive.
    /// <br/>Use SystemLabels on EndUserContext instead.
    /// </summary>
    [JsonPropertyName("systemLabel")]
    [JsonConverter(typeof(JsonStringEnumConverter<SystemLabel>))]
    [Obsolete("Use EndUserContext.SystemLabels instead.")]
    public SystemLabel SystemLabel { get; set; } = default!;

    /// <summary>
    /// Indicates if this dialog is intended for API consumption only and should not be shown in frontends aimed at humans.
    /// </summary>
    [JsonPropertyName("isApiOnly")]
    public bool IsApiOnly { get; set; } = false;

    /// <summary>
    /// Whether the service owner has not yet reported all dialog Transmissions they sent as seen by the end user.
    /// <br/>A Transmission is considered "sent from the service owner" if the DialogTransmissionType is not one of Submission or Correction.
    /// <br/>            
    /// <br/>The value of this field is:
    /// <br/>- true when there are any new unopened Transmissions sent from the service owner.
    /// <br/>- false when the service owner has created an Activity of type TransmissionOpened for all Transmissions sent from the service owner. The Activities must each contain the relevant Id for all relevant Transmissions.
    /// <br/>            
    /// <br/>Note that the value is
    /// <br/>- determined by the service owner and not to be confused with IsContentSeen
    /// <br/>- not affected by SystemLabels
    /// <br/>            
    /// <br/>For correspondence: HasUnopenedContent is still true until the service owner also adds a Dialog level Activity (no transmission id) of type CorrespondenceOpened
    /// </summary>
    [JsonPropertyName("hasUnopenedContent")]
    public bool HasUnopenedContent { get; set; } = false;

    /// <summary>
    /// The dialog unstructured text content.
    /// </summary>
    [JsonPropertyName("content")]
    public Content Content { get; set; } = null!;

    /// <summary>
    /// The dialog token. May be used (if supported) against external URLs referred to in this dialog's apiActions,
    /// <br/>transmissions or attachments. It should also be used for front-channel embeds.
    /// </summary>
    [JsonPropertyName("dialogToken")]
    public string? DialogToken { get; set; } = null!;

    /// <summary>
    /// The number of transmissions sent by a service owner.
    /// </summary>
    [JsonPropertyName("fromServiceOwnerTransmissionsCount")]
    public int FromServiceOwnerTransmissionsCount { get; set; } = 0;

    /// <summary>
    /// The number of transmissions sent by a party representative.
    /// </summary>
    [JsonPropertyName("fromPartyTransmissionsCount")]
    public int FromPartyTransmissionsCount { get; set; } = 0;

    /// <summary>
    /// The attachments associated with the dialog (on an aggregate level).
    /// </summary>
    [JsonPropertyName("attachments")]
    public ICollection<DialogAttachment>? Attachments { get; set; } = null!;

    /// <summary>
    /// The immutable list of transmissions associated with the dialog.
    /// </summary>
    [JsonPropertyName("transmissions")]
    public ICollection<DialogTransmission>? Transmissions { get; set; } = null!;

    /// <summary>
    /// The GUI actions associated with the dialog. Should be used in browser-based interactive frontends.
    /// </summary>
    [JsonPropertyName("guiActions")]
    public ICollection<DialogGuiAction>? GuiActions { get; set; } = null!;

    /// <summary>
    /// The API actions associated with the dialog. Should be used in specialized, non-browser-based integrations.
    /// </summary>
    [JsonPropertyName("apiActions")]
    public ICollection<DialogApiAction>? ApiActions { get; set; } = null!;

    /// <summary>
    /// An immutable list of activities associated with the dialog.
    /// </summary>
    [JsonPropertyName("activities")]
    public ICollection<DialogActivity>? Activities { get; set; } = null!;

    /// <summary>
    /// The list of seen log entries for the dialog newer than the dialog UpdatedAt date.
    /// </summary>
    [JsonPropertyName("seenSinceLastUpdate")]
    public ICollection<DialogSeenLog>? SeenSinceLastUpdate { get; set; } = null!;

    /// <summary>
    /// The list of seen log entries for the dialog newer than the dialog ContentUpdatedAt date.
    /// </summary>
    [JsonPropertyName("seenSinceLastContentUpdate")]
    public ICollection<DialogSeenLog>? SeenSinceLastContentUpdate { get; set; } = null!;

    /// <summary>
    /// Indicates whether a dialog has been seen since its last content update.
    /// <br/>            
    /// <br/>The value of this field is
    /// <br/>- true if the dialog has been retrieved since its last content update by either GET /enduser/dialogs/{dialogId} or GET /serviceowner/dialogs/{dialogId}?EndUserId={userId} and there is no SystemLabels MarkedAsUnopened
    /// <br/>- false if there is a SystemLabels MarkedAsUnopened, even if the dialog has been seen since its last content update
    /// <br/>- false after the dialog receives a content update.
    /// <br/>            
    /// <br/>Note that the value is determined by Dialogporten and not to be confused with HasUnopenedContent
    /// </summary>
    [JsonPropertyName("isContentSeen")]
    public bool IsContentSeen { get; set; } = false;

    /// <summary>
    /// Metadata about the dialog owned by end-users.
    /// </summary>
    [JsonPropertyName("endUserContext")]
    public DialogEndUserContext EndUserContext { get; set; } = null!;

}

public class Content
{

    /// <summary>
    /// The title of the dialog.
    /// </summary>
    [JsonPropertyName("title")]
    public ContentValue Title { get; set; } = null!;

    /// <summary>
    /// A short summary of the dialog and its current state.
    /// </summary>
    [JsonPropertyName("summary")]
    public ContentValue? Summary { get; set; } = null!;

    /// <summary>
    /// Overridden sender name. If not supplied, assume "org" as the sender name.
    /// </summary>
    [JsonPropertyName("senderName")]
    public ContentValue? SenderName { get; set; } = null!;

    /// <summary>
    /// Additional information about the dialog, this may contain Markdown.
    /// </summary>
    [JsonPropertyName("additionalInfo")]
    public ContentValue? AdditionalInfo { get; set; } = null!;

    /// <summary>
    /// Used as the human-readable label used to describe the "ExtendedStatus" field.
    /// </summary>
    [JsonPropertyName("extendedStatus")]
    public ContentValue? ExtendedStatus { get; set; } = null!;

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL.
    /// <br/>Content value will be masked if the user is not authorized to read main content.
    /// </summary>
    [JsonPropertyName("mainContentReference")]
    public ContentValue? MainContentReference { get; set; } = null!;

}

public class DialogAttachment
{

    /// <summary>
    /// The unique identifier for the attachment in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The display name of the attachment that should be used in GUIs.
    /// </summary>
    [JsonPropertyName("displayName")]
    public ICollection<Localization>? DisplayName { get; set; } = null!;

    /// <summary>
    /// The logical name of the attachment.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; } = null!;

    /// <summary>
    /// The URLs associated with the attachment, each referring to a different representation of the attachment.
    /// </summary>
    [JsonPropertyName("urls")]
    public ICollection<DialogAttachmentUrl>? Urls { get; set; } = null!;

    /// <summary>
    /// The UTC timestamp when the attachment expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; } = null!;

}

public class DialogAttachmentUrl
{

    /// <summary>
    /// The unique identifier for the attachment URL in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The fully qualified URL of the attachment.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The media type of the attachment.
    /// </summary>
    [JsonPropertyName("mediaType")]
    public string? MediaType { get; set; } = null!;

    /// <summary>
    /// What type of consumer the URL is intended for.
    /// </summary>
    [JsonPropertyName("consumerType")]
    [JsonConverter(typeof(JsonStringEnumConverter<AttachmentUrlConsumerType>))]
    public AttachmentUrlConsumerType ConsumerType { get; set; } = default!;

}

public class DialogTransmission
{

    /// <summary>
    /// The unique identifier for the transmission in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The date and time when the transmission was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = default!;

    /// <summary>
    /// Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service
    /// <br/>policy, which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    /// <br/>            
    /// <br/>Can also be used to refer to other service policies.
    /// </summary>
    [JsonPropertyName("authorizationAttribute")]
    public string? AuthorizationAttribute { get; set; } = null!;

    /// <summary>
    /// Flag indicating if the authenticated user is authorized for this transmission. If not, embedded content and
    /// <br/>the attachments will not be available.
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool IsAuthorized { get; set; } = false;

    /// <summary>
    /// Arbitrary URI/URN describing a service-specific transmission type.
    /// <br/>            
    /// <br/>Refer to the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; } = null!;

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// </summary>
    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; } = null!;

    /// <summary>
    /// Reference to any other transmission that this transmission is related to.
    /// </summary>
    [JsonPropertyName("relatedTransmissionId")]
    public Guid? RelatedTransmissionId { get; set; } = null!;

    /// <summary>
    /// The type of transmission.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogTransmissionType>))]
    public DialogTransmissionType Type { get; set; } = default!;

    /// <summary>
    /// The actor that sent the transmission.
    /// </summary>
    [JsonPropertyName("sender")]
    public Actor Sender { get; set; } = null!;

    /// <summary>
    /// Indicates whether the dialog transmission has been opened.
    /// </summary>
    [JsonPropertyName("isOpened")]
    public bool IsOpened { get; set; } = false;

    /// <summary>
    /// The transmission unstructured text content.
    /// </summary>
    [JsonPropertyName("content")]
    public DialogTransmissionContent Content { get; set; } = null!;

    /// <summary>
    /// The transmission-level attachments.
    /// </summary>
    [JsonPropertyName("attachments")]
    public ICollection<DialogTransmissionAttachment>? Attachments { get; set; } = null!;

    /// <summary>
    /// The transmission-level navigational actions.
    /// </summary>
    [JsonPropertyName("navigationalActions")]
    public ICollection<DialogTransmissionNavigationalAction>? NavigationalActions { get; set; } = null!;

}

public class DialogTransmissionContent
{

    /// <summary>
    /// The transmission title.
    /// </summary>
    [JsonPropertyName("title")]
    public ContentValue Title { get; set; } = null!;

    /// <summary>
    /// The transmission summary.
    /// </summary>
    [JsonPropertyName("summary")]
    public ContentValue? Summary { get; set; } = null!;

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL.
    /// </summary>
    [JsonPropertyName("contentReference")]
    public ContentValue? ContentReference { get; set; } = null!;

}

public class DialogTransmissionAttachment
{

    /// <summary>
    /// The unique identifier for the attachment in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The display name of the attachment that should be used in GUIs.
    /// </summary>
    [JsonPropertyName("displayName")]
    public ICollection<Localization>? DisplayName { get; set; } = null!;

    /// <summary>
    /// The logical name of the attachment.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; } = null!;

    /// <summary>
    /// The URLs associated with the attachment, each referring to a different representation of the attachment.
    /// </summary>
    [JsonPropertyName("urls")]
    public ICollection<DialogTransmissionAttachmentUrl>? Urls { get; set; } = null!;

    /// <summary>
    /// The UTC timestamp when the attachment expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; } = null!;

}

public class DialogTransmissionAttachmentUrl
{

    /// <summary>
    /// The unique identifier for the attachment URL in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The fully qualified URL of the attachment. Will be set to "urn:dialogporten:unauthorized" if the user is
    /// <br/>not authorized to access the transmission.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The media type of the attachment.
    /// </summary>
    [JsonPropertyName("mediaType")]
    public string? MediaType { get; set; } = null!;

    /// <summary>
    /// The type of consumer the URL is intended for.
    /// </summary>
    [JsonPropertyName("consumerType")]
    [JsonConverter(typeof(JsonStringEnumConverter<AttachmentUrlConsumerType>))]
    public AttachmentUrlConsumerType ConsumerType { get; set; } = default!;

}

public class DialogTransmissionNavigationalAction
{

    /// <summary>
    /// The title of the navigational action.
    /// </summary>
    [JsonPropertyName("title")]
    public ICollection<Localization>? Title { get; set; } = null!;

    /// <summary>
    /// The fully qualified URL of the navigational action. Will be set to \"urn:dialogporten:unauthorized\" if the user is
    /// <br/>not authorized to access the transmission, or \"urn:dialogporten:expired\" if the action has expired.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The UTC timestamp when the navigational action expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; } = null!;

}

public class DialogGuiAction
{

    /// <summary>
    /// The unique identifier for the action in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The action identifier for the action, corresponding to the "action" attributeId used in the XACML service policy.
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = null!;

    /// <summary>
    /// The fully qualified URL of the action, to which the user will be redirected when the action is triggered. Will be set to
    /// <br/>"urn:dialogporten:unauthorized" if the user is not authorized to perform the action.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service
    /// <br/>policy, which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    /// <br/>            
    /// <br/>Can also be used to refer to other service policies.
    /// </summary>
    [JsonPropertyName("authorizationAttribute")]
    public string? AuthorizationAttribute { get; set; } = null!;

    /// <summary>
    /// Whether the user is authorized to perform the action.
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool IsAuthorized { get; set; } = false;

    /// <summary>
    /// Indicates whether the action results in the dialog being deleted. Used by frontends to implement custom UX
    /// <br/>for delete actions.
    /// </summary>
    [JsonPropertyName("isDeleteDialogAction")]
    public bool IsDeleteDialogAction { get; set; } = false;

    /// <summary>
    /// Indicates a priority for the action, making it possible for frontends to adapt GUI elements based on action
    /// <br/>priority.
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogGuiActionPriority>))]
    public DialogGuiActionPriority Priority { get; set; } = default!;

    /// <summary>
    /// The HTTP method that the frontend should use when redirecting the user.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    [JsonConverter(typeof(JsonStringEnumConverter<HttpVerb>))]
    public HttpVerb HttpMethod { get; set; } = default!;

    /// <summary>
    /// The title of the action, this should be short and in verb form.
    /// </summary>
    [JsonPropertyName("title")]
    public ICollection<Localization>? Title { get; set; } = null!;

    /// <summary>
    /// If there should be a prompt asking the user for confirmation before the action is executed,
    /// <br/>this field should contain the prompt text.
    /// </summary>
    [JsonPropertyName("prompt")]
    public ICollection<Localization>? Prompt { get; set; } = null!;

}

public enum DialogGuiActionPriority
{

    [System.Runtime.Serialization.EnumMember(Value = @"Primary")]
    Primary = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Secondary")]
    Secondary = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"Tertiary")]
    Tertiary = 2,

}

public enum HttpVerb
{

    [System.Runtime.Serialization.EnumMember(Value = @"GET")]
    Get = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"POST")]
    Post = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"PUT")]
    Put = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"PATCH")]
    Patch = 3,

    [System.Runtime.Serialization.EnumMember(Value = @"DELETE")]
    Delete = 4,

    [System.Runtime.Serialization.EnumMember(Value = @"HEAD")]
    Head = 5,

    [System.Runtime.Serialization.EnumMember(Value = @"OPTIONS")]
    Options = 6,

    [System.Runtime.Serialization.EnumMember(Value = @"TRACE")]
    Trace = 7,

    [System.Runtime.Serialization.EnumMember(Value = @"CONNECT")]
    Connect = 8,

}

public class DialogApiAction
{

    /// <summary>
    /// The unique identifier for the action in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// String identifier for the action, corresponding to the "action" attributeId used in the XACML service policy,
    /// <br/>which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = null!;

    /// <summary>
    /// Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service
    /// <br/>policy, which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    /// <br/>            
    /// <br/>Can also be used to refer to other service policies.
    /// </summary>
    [JsonPropertyName("authorizationAttribute")]
    public string? AuthorizationAttribute { get; set; } = null!;

    /// <summary>
    /// True if the authenticated user is authorized for this action. If not, the action will not be available
    /// <br/>and all endpoints will be replaced with a fixed placeholder.
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool IsAuthorized { get; set; } = false;

    /// <summary>
    /// The logical name of the operation the API action refers to.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; } = null!;

    /// <summary>
    /// The endpoints associated with the action.
    /// </summary>
    [JsonPropertyName("endpoints")]
    public ICollection<DialogApiActionEndpoint>? Endpoints { get; set; } = null!;

}

public class DialogApiActionEndpoint
{

    /// <summary>
    /// The unique identifier for the endpoint in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// Arbitrary string indicating the version of the endpoint.
    /// <br/>            
    /// <br/>Consult the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; } = null!;

    /// <summary>
    /// The fully qualified URL of the API endpoint. Will be set to "urn:dialogporten:unauthorized" if the user is
    /// <br/>not authorized to perform the action.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The HTTP method that the endpoint expects for this action.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    [JsonConverter(typeof(JsonStringEnumConverter<HttpVerb>))]
    public HttpVerb HttpMethod { get; set; } = default!;

    /// <summary>
    /// Link to service provider documentation for the endpoint. Used for service owners to provide documentation for
    /// <br/>integrators. Should be a URL to a human-readable page.
    /// </summary>
    [JsonPropertyName("documentationUrl")]
    public Uri? DocumentationUrl { get; set; } = null!;

    /// <summary>
    /// Link to the request schema for the endpoint. Used by service owners to provide documentation for integrators.
    /// <br/>Dialogporten will not validate information on this endpoint.
    /// </summary>
    [JsonPropertyName("requestSchema")]
    public Uri? RequestSchema { get; set; } = null!;

    /// <summary>
    /// Link to the response schema for the endpoint. Used for service owners to provide documentation for integrators.
    /// <br/>Dialogporten will not validate information on this endpoint.
    /// </summary>
    [JsonPropertyName("responseSchema")]
    public Uri? ResponseSchema { get; set; } = null!;

    /// <summary>
    /// Boolean indicating if the endpoint is deprecated. Integrators should migrate to endpoints with a higher version.
    /// </summary>
    [JsonPropertyName("deprecated")]
    public bool Deprecated { get; set; } = false;

    /// <summary>
    /// Date and time when the service owner has indicated that endpoint will no longer function. Only set if the endpoint
    /// <br/>is deprecated. Dialogporten will not enforce this date.
    /// </summary>
    [JsonPropertyName("sunsetAt")]
    public DateTimeOffset? SunsetAt { get; set; } = null!;

}

public class DialogActivity
{

    /// <summary>
    /// The unique identifier for the activity in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The date and time when the activity was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; } = null!;

    /// <summary>
    /// An arbitrary URI/URN with a service-specific activity type.
    /// <br/>            
    /// <br/>Consult the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; } = null!;

    /// <summary>
    /// The type of activity.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogActivityType>))]
    public DialogActivityType Type { get; set; } = default!;

    /// <summary>
    /// If the activity is related to a particular transmission, this field will contain the transmission identifier.
    /// </summary>
    [JsonPropertyName("transmissionId")]
    public Guid? TransmissionId { get; set; } = null!;

    /// <summary>
    /// The actor that performed the activity.
    /// </summary>
    [JsonPropertyName("performedBy")]
    public Actor PerformedBy { get; set; } = null!;

    /// <summary>
    /// Unstructured text describing the activity. Only set if the activity type is "Information".
    /// </summary>
    [JsonPropertyName("description")]
    public ICollection<Localization>? Description { get; set; } = null!;

}

public class DialogSeenLog
{

    /// <summary>
    /// The unique identifier for the seen log entry in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The timestamp when the dialog revision was seen.
    /// </summary>
    [JsonPropertyName("seenAt")]
    public DateTimeOffset SeenAt { get; set; } = default!;

    /// <summary>
    /// The actor that saw the dialog revision.
    /// </summary>
    [JsonPropertyName("seenBy")]
    public Actor SeenBy { get; set; } = null!;

    /// <summary>
    /// Flag indicating whether the seen log entry was created via the service owner.
    /// <br/>            
    /// <br/>This is used when the service owner uses the service owner API to implement its own frontend.
    /// </summary>
    [JsonPropertyName("isViaServiceOwner")]
    public bool? IsViaServiceOwner { get; set; } = null!;

    /// <summary>
    /// Flag indicating whether the seen log entry was created by the current end user.
    /// </summary>
    [JsonPropertyName("isCurrentEndUser")]
    public bool IsCurrentEndUser { get; set; } = false;

}

public class DialogEndUserContext
{

    /// <summary>
    /// The unique identifier for the end user context revision in UUIDv4 format.
    /// </summary>
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; } = Guid.Empty;

    /// <summary>
    /// System defined labels used to categorize dialogs.
    /// </summary>
    [JsonPropertyName("systemLabels")]
    public ICollection<SystemLabel>? SystemLabels { get; set; } = null!;

}

public class EndUserIdentifierLookup
{

    [JsonPropertyName("dialogId")]
    public Guid DialogId { get; set; } = Guid.Empty;

    [JsonPropertyName("instanceRef")]
    public string InstanceRef { get; set; } = null!;

    [JsonPropertyName("party")]
    public string Party { get; set; } = null!;

    [JsonPropertyName("serviceResource")]
    public IdentifierLookupServiceResource ServiceResource { get; set; } = null!;

    [JsonPropertyName("serviceOwner")]
    public IdentifierLookupServiceOwner ServiceOwner { get; set; } = null!;

    [JsonPropertyName("title")]
    public ICollection<Localization>? Title { get; set; } = null!;

    [JsonPropertyName("authorizationEvidence")]
    public IdentifierLookupAuthorizationEvidence AuthorizationEvidence { get; set; } = null!;

}

public class IdentifierLookupServiceResource
{

    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("isDelegable")]
    public bool IsDelegable { get; set; } = false;

    [JsonPropertyName("minimumAuthenticationLevel")]
    public int MinimumAuthenticationLevel { get; set; } = 0;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = null!;

}

public class IdentifierLookupServiceOwner
{

    [JsonPropertyName("orgNumber")]
    public string OrgNumber { get; set; } = null!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = null!;

}

public class IdentifierLookupAuthorizationEvidence
{

    [JsonPropertyName("currentAuthenticationLevel")]
    public int CurrentAuthenticationLevel { get; set; } = 0;

    [JsonPropertyName("viaRole")]
    public bool ViaRole { get; set; } = false;

    [JsonPropertyName("viaAccessPackage")]
    public bool ViaAccessPackage { get; set; } = false;

    [JsonPropertyName("viaResourceDelegation")]
    public bool ViaResourceDelegation { get; set; } = false;

    [JsonPropertyName("viaInstanceDelegation")]
    public bool ViaInstanceDelegation { get; set; } = false;

    [JsonPropertyName("evidence")]
    public ICollection<IdentifierLookupAuthorizationEvidenceItem>? Evidence { get; set; } = null;

}

public class IdentifierLookupAuthorizationEvidenceItem
{

    [JsonPropertyName("grantType")]
    [JsonConverter(typeof(JsonStringEnumConverter<IdentifierLookupGrantType>))]
    public IdentifierLookupGrantType GrantType { get; set; } = default!;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = null!;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = null!;

    [JsonPropertyName("links")]
    public Links? Links { get; set; } = null!;

}

public enum IdentifierLookupGrantType
{

    [System.Runtime.Serialization.EnumMember(Value = @"Role")]
    Role = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"AccessPackage")]
    AccessPackage = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"ResourceDelegation")]
    ResourceDelegation = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"InstanceDelegation")]
    InstanceDelegation = 3,

}

public class Parties
{

    [JsonPropertyName("authorizedParties")]
    public ICollection<AuthorizedParty>? AuthorizedParties { get; set; } = null;

}

public class AuthorizedParty
{

    /// <summary>
    /// The party identifier in URN format
    /// </summary>
    [JsonPropertyName("party")]
    public string Party { get; set; } = null!;

    /// <summary>
    /// The UUID for the party.
    /// </summary>
    [JsonPropertyName("partyUuid")]
    public Guid PartyUuid { get; set; } = Guid.Empty;

    /// <summary>
    /// The numeric identifier for the party.
    /// </summary>
    [JsonPropertyName("partyId")]
    public int PartyId { get; set; } = 0;

    /// <summary>
    /// The name of the party (verbatim from CCR, usually in all caps)
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The date of birth of the party, if a person.
    /// </summary>
    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; set; } = null!;

    /// <summary>
    /// The type of the party, either "Organization" or "Person".
    /// </summary>
    [JsonPropertyName("partyType")]
    public string PartyType { get; set; } = null!;

    /// <summary>
    /// Whether the party is deleted or not
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Whether the authenticated user has a key role in the party.
    /// <br/>            
    /// <br/>Read more about key roles (norwegian) at https://docs.altinn.studio/nb/altinn-studio/reference/configuration/authorization/guidelines_authorization/roles_and_rights/roles_er/#nøkkelroller
    /// </summary>
    [JsonPropertyName("hasKeyRole")]
    public bool HasKeyRole { get; set; } = false;

    /// <summary>
    /// Whether this party represents the authenticated user.
    /// </summary>
    [JsonPropertyName("isCurrentEndUser")]
    public bool IsCurrentEndUser { get; set; } = false;

    /// <summary>
    /// Whether the authenticated user is the main administrator of the party
    /// <br/>            
    /// <br/>Read more about main administrator (norwegian) at https://docs.altinn.studio/nb/altinn-studio/reference/configuration/authorization/guidelines_authorization/roles_and_rights/roles_altinn/altinn_roles_administration/#hovedadministrator
    /// </summary>
    [JsonPropertyName("isMainAdministrator")]
    public bool IsMainAdministrator { get; set; } = false;

    /// <summary>
    /// Whether the authenticated user is an access manager of the party.
    /// <br/>            
    /// <br/>Read more about access managers (norwegian) at https://docs.altinn.studio/nb/altinn-studio/reference/configuration/authorization/guidelines_authorization/roles_and_rights/roles_altinn/altinn_roles_administration/#tilgangsstrying
    /// </summary>
    [JsonPropertyName("isAccessManager")]
    public bool IsAccessManager { get; set; } = false;

    /// <summary>
    /// If the authenticated user has only access to sub parties of this party, and not this party itself.
    /// </summary>
    [JsonPropertyName("hasOnlyAccessToSubParties")]
    public bool HasOnlyAccessToSubParties { get; set; } = false;

    /// <summary>
    /// The sub parties of this party, if any. The sub party uses the same data model.
    /// </summary>
    [JsonPropertyName("subParties")]
    public ICollection<AuthorizedParty>? SubParties { get; set; } = null!;

}
