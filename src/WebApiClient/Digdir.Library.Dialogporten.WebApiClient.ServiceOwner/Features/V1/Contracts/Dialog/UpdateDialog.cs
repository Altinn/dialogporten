using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Dialog;

public class UpdateDialog
{
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
    /// <br/>If any Transmissions were created without Content while this property was true, the flag cannot be reverted to false.
    /// </summary>
    [JsonPropertyName("isApiOnly")]
    public bool IsApiOnly { get; set; }

    /// <summary>
    /// The aggregated status of the dialog.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogStatusInput>))]
    public DialogStatusInput Status { get; set; }

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
    /// The immutable list of transmissions associated with the dialog. When updating via PUT, any transmissions
    /// <br/>added here will be appended to the existing list of transmissions.
    /// </summary>
    [JsonPropertyName("transmissions")]
    public ICollection<CreateTransmissionRequest> Transmissions { get; set; } = [];

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
    /// An immutable list of activities associated with the dialog. When updating via PUT, any activities added here
    /// <br/>will be appended to the existing list of activities.
    /// </summary>
    [JsonPropertyName("activities")]
    public ICollection<DialogActivity> Activities { get; set; } = [];
}
