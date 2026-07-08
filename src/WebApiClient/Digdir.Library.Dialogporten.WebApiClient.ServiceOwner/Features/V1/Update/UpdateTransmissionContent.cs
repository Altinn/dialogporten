using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Update;

public class UpdateTransmissionContent
{
    /// <summary>
    /// The transmission title. Must be text/plain.
    /// </summary>
    [JsonPropertyName("title")]
    public ContentValue Title { get; set; } = default!;

    /// <summary>
    /// The transmission summary.
    /// </summary>
    [JsonPropertyName("summary")]
    public ContentValue? Summary { get; set; }

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL. Must be HTTPS.
    /// </summary>
    [JsonPropertyName("contentReference")]
    public ContentValue? ContentReference { get; set; }
}
