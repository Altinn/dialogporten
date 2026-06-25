using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Transmission;

public class TransmissionContent
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
    public ContentValue? Summary { get; set; }

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL.
    /// </summary>
    [JsonPropertyName("contentReference")]
    public ContentValue? ContentReference { get; set; }
}