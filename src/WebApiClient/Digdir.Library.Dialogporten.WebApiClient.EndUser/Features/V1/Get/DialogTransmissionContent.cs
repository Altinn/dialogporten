using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Get;

public class DialogTransmissionContent
{
    /// <summary>
    /// The transmission title.
    /// </summary>
    [JsonPropertyName("title")]
    public required ContentValue Title { get; set; }

    /// <summary>
    /// The transmission summary.
    /// </summary>
    [JsonPropertyName("summary")]
    public ContentValue? Summary { get; set; }

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL.
    /// </summary>
    [JsonPropertyName("contentReference")]
    public ContentValue? ContentReference { get; set; }
}
