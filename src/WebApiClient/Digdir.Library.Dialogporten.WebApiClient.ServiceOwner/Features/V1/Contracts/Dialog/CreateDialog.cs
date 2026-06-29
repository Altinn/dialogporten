using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Dialog;

public class CreateDialog
{
    /// <summary>
    /// A self-defined UUIDv7 may be provided to support idempotent creation of dialogs. If not provided, a new UUIDv7 will be generated.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// An optional key to ensure idempotency in dialog creation. If provided, it allows for the safe re-submission of the same dialog creation request without creating duplicate entries.
    /// </summary>
    [JsonPropertyName("idempotentKey")]
    public string? IdempotentKey { get; set; }

    /// <summary>
    /// The service identifier for the service that the dialog is related to in URN-format.
    /// <br/>This corresponds to a resource in the Altinn Resource Registry, which the authenticated organization
    /// <br/>must own, i.e., be listed as the "competent authority" in the Resource Registry entry.
    /// </summary>
    [JsonPropertyName("serviceResource")]
    public string ServiceResource { get; set; } = null!;

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
    /// Arbitrary string with a service-specific indicator of status, typically used to indicate a fine-grained state of
    /// <br/>the dialog to further specify the "status" enum.
    /// </summary>
    [JsonPropertyName("extendedStatus")]
    public string? ExtendedStatus { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// </summary>
    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; }

    /// <summary>
    /// The timestamp when the dialog should be made visible for authorized end users. If not provided, the dialog will be
    /// <br/>immediately available.
    /// </summary>
    [JsonPropertyName("visibleFrom")]
    public DateTimeOffset? VisibleFrom { get; set; }

    /// <summary>
    /// The due date for the dialog. Dialogs past due date might be marked as such in frontends but will still be available.
    /// </summary>
    [JsonPropertyName("dueAt")]
    public DateTimeOffset? DueAt { get; set; }

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
    /// The expiration date for the dialog. This is the last date when the dialog is available for the end user.
    /// <br/>
    /// <br/>After this date is passed, the dialog will be considered expired and no longer available for the end user in any
    /// <br/>API. If not supplied, the dialog will be considered to never expire. This field can be changed after creation.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Indicates if this dialog is intended for API consumption only and should not be displayed in user interfaces.
    /// <br/>When true, the dialog will not be visible in portals designed for human users, but will remain accessible via API.
    /// </summary>
    [JsonPropertyName("isApiOnly")]
    public bool IsApiOnly { get; set; }

    /// <summary>
    /// If set, will override the date and time when the dialog is set as created.
    /// <br/>If not supplied, the current date /time will be used.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// If set, will override the date and time when the dialog is set as last updated.
    /// <br/>If not supplied, the current date /time will be used.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// The aggregated status of the dialog.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogStatusInput>))]
    public DialogStatusInput? Status { get; set; }

    /// <summary>
    /// Set the system label of the dialog.
    /// </summary>
    [JsonPropertyName("systemLabel")]
    [JsonConverter(typeof(JsonStringEnumConverter<SystemLabel>))]
    public SystemLabel? SystemLabel { get; set; }

    /// <summary>
    /// Metadata about the dialog owned by the service owner.
    /// </summary>
    [JsonPropertyName("serviceOwnerContext")]
    public CreateDialogServiceOwnerContext? ServiceOwnerContext { get; set; }

    /// <summary>
    /// The dialog unstructured text content.
    /// </summary>
    [JsonPropertyName("content")]
    public DialogContent? Content { get; set; }

    /// <summary>
    /// A list of words (tags) that will be used in dialog search queries. Not visible in end-user DTO.
    /// </summary>
    [JsonPropertyName("searchTags")]
    public ICollection<DialogTag> SearchTags { get; set; } = [];

    /// <summary>
    /// The attachments associated with the dialog (on an aggregate level).
    /// </summary>
    [JsonPropertyName("attachments")]
    public ICollection<DialogAttachment> Attachments { get; set; } = [];

    /// <summary>
    /// The immutable list of transmissions associated with the dialog.
    /// </summary>
    [JsonPropertyName("transmissions")]
    public ICollection<CreateTransmissionRequest>? Transmissions { get; set; }

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
    public ICollection<DialogActivity>? Activities { get; set; }
}
