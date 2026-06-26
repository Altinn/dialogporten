using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Dialog;

public class DialogSearchContent
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
    public ContentValue? Summary { get; set; }

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
