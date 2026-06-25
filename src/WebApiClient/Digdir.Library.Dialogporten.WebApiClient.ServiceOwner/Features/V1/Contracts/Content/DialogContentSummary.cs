using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Content;

public class DialogContentSummary
{
    /// <summary>
    /// The title of the dialog.
    /// </summary>
    [JsonPropertyName("title")]
    public ContentValue Title { get; set; } = null!;

    /// <summary>
    /// An optional non-sensitive title of the dialog.
    /// <br/>Used for search and list views if the user authorization does not meet the required eIDAS level
    /// </summary>
    [JsonPropertyName("nonSensitiveTitle")]
    public ContentValue? NonSensitiveTitle { get; set; }

    /// <summary>
    /// A short summary of the dialog and its current state.
    /// </summary>
    [JsonPropertyName("summary")]
    public ContentValue? Summary { get; set; }

    /// <summary>
    /// An optional non-sensitive summary of the dialog and its current state.
    /// <br/>Used for search and list views if the user authorization does not meet the required eIDAS level
    /// </summary>
    [JsonPropertyName("nonSensitiveSummary")]
    public ContentValue? NonSensitiveSummary { get; set; }

    /// <summary>
    /// Overridden sender name. If not supplied, assume "org" as the sender name.
    /// </summary>
    [JsonPropertyName("senderName")]
    public ContentValue? SenderName { get; set; }

    /// <summary>
    /// Used as the human-readable label used to describe the "ExtendedStatus" field.
    /// </summary>
    [JsonPropertyName("extendedStatus")]
    public ContentValue? ExtendedStatus { get; set; }
}