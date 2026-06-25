using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Transmission;

public class DialogTransmissionNavigationalAction
{
    /// <summary>
    /// The title of the navigational action.
    /// </summary>
    [JsonPropertyName("title")]
    public ICollection<Localization>? Title { get; set; }

    /// <summary>
    /// The fully qualified URL of the navigational action.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The UTC timestamp when the navigational action expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }
}