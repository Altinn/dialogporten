using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Dialog;

public class Dialog
{
    /// <summary>
    /// The unique identifier for the dialog in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier for the revision in UUIDv4 format.
    /// </summary>
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }

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
    public int? Progress { get; set; }

    /// <summary>
    /// Optional process identifier used to indicate a business process this dialog belongs to.
    /// </summary>
    [JsonPropertyName("process")]
    public string? Process { get; set; }

    /// <summary>
    /// Optional preceding process identifier to indicate the business process that preceded the process indicated in the "Process" field. Cannot be set without also "Process" being set.
    /// </summary>
    [JsonPropertyName("precedingProcess")]
    public string? PrecedingProcess { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific indicator of status, typically used to indicate a fine-grained state of
    /// <br/>the dialog to further specify the "status" enum.
    /// <br/>
    /// <br/>Refer to the service-specific documentation provided by the service owner for details on the possible values (if
    /// <br/>in use).
    /// </summary>
    [JsonPropertyName("extendedStatus")]
    public string? ExtendedStatus { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// <br/>
    /// <br/>Refer to the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; }

    /// <summary>
    /// The due date for the dialog. Dialogs past due date might be marked as such in frontends but will still be available.
    /// </summary>
    [JsonPropertyName("dueAt")]
    public DateTimeOffset? DueAt { get; set; }

    /// <summary>
    /// The expiration date for the dialog. This is the last date when the dialog is available for the end user.
    /// <br/>
    /// <br/>After this date is passed, the dialog will be considered expired and no longer available for the end user in any
    /// <br/>API. If not supplied, the dialog will be considered to never expire. This field can be changed by the service
    /// <br/>owner after the dialog has been created.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// The date and time when the dialog was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the dialog was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// The date and time when the dialog content was last updated.
    /// </summary>
    [JsonPropertyName("contentUpdatedAt")]
    public DateTimeOffset ContentUpdatedAt { get; set; }

    /// <summary>
    /// The aggregated status of the dialog.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogStatus>))]
    public DialogStatus Status { get; set; }

    /// <summary>
    /// System defined label used to categorize dialogs.
    /// <br/>This is obsolete and will only show; Default, Bin or Archive.
    /// <br/>Use SystemLabels on EndUserContext instead.
    /// </summary>
    [JsonPropertyName("systemLabel")]
    [JsonConverter(typeof(JsonStringEnumConverter<SystemLabel>))]
    [Obsolete("Use EndUserContext.SystemLabels instead.")]
    public SystemLabel SystemLabel { get; set; }

    /// <summary>
    /// Indicates if this dialog is intended for API consumption only and should not be shown in frontends aimed at humans.
    /// </summary>
    [JsonPropertyName("isApiOnly")]
    public bool IsApiOnly { get; set; }

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
    public bool HasUnopenedContent { get; set; }

    /// <summary>
    /// The dialog unstructured text content.
    /// </summary>
    [JsonPropertyName("content")]
    public DialogContent DialogContent { get; set; } = null!;

    /// <summary>
    /// The dialog token. May be used (if supported) against external URLs referred to in this dialog's apiActions,
    /// <br/>transmissions or attachments. It should also be used for front-channel embeds.
    /// </summary>
    [JsonPropertyName("dialogToken")]
    public string? DialogToken { get; set; }

    /// <summary>
    /// The number of transmissions sent by a service owner.
    /// </summary>
    [JsonPropertyName("fromServiceOwnerTransmissionsCount")]
    public int FromServiceOwnerTransmissionsCount { get; set; }

    /// <summary>
    /// The number of transmissions sent by a party representative.
    /// </summary>
    [JsonPropertyName("fromPartyTransmissionsCount")]
    public int FromPartyTransmissionsCount { get; set; }

    /// <summary>
    /// The attachments associated with the dialog (on an aggregate level).
    /// </summary>
    [JsonPropertyName("attachments")]
    public ICollection<DialogAttachment> Attachments { get; set; } = [];

    /// <summary>
    /// The immutable list of transmissions associated with the dialog.
    /// </summary>
    [JsonPropertyName("transmissions")]
    public ICollection<DialogTransmission> Transmissions { get; set; } = [];

    /// <summary>
    /// The GUI actions associated with the dialog. Should be used in browser-based interactive frontends.
    /// </summary>
    [JsonPropertyName("guiActions")]
    public ICollection<DialogGuiAction> GuiActions { get; set; } = [];

    /// <summary>
    /// The API actions associated with the dialog. Should be used in specialized, non-browser-based integrations.
    /// </summary>
    [JsonPropertyName("apiActions")]
    public ICollection<DialogApiAction> ApiActions { get; set; } = [];

    /// <summary>
    /// An immutable list of activities associated with the dialog.
    /// </summary>
    [JsonPropertyName("activities")]
    public ICollection<DialogActivity> Activities { get; set; } = [];

    /// <summary>
    /// The list of seen log entries for the dialog newer than the dialog UpdatedAt date.
    /// </summary>
    [JsonPropertyName("seenSinceLastUpdate")]
    public ICollection<DialogSeenLog> SeenSinceLastUpdate { get; set; } = [];

    /// <summary>
    /// The list of seen log entries for the dialog newer than the dialog ContentUpdatedAt date.
    /// </summary>
    [JsonPropertyName("seenSinceLastContentUpdate")]
    public ICollection<DialogSeenLog> SeenSinceLastContentUpdate { get; set; } = [];

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
    public bool IsContentSeen { get; set; }

    /// <summary>
    /// Metadata about the dialog owned by end-users.
    /// </summary>
    [JsonPropertyName("endUserContext")]
    public DialogEndUserContext EndUserContext { get; set; } = null!;
}
