using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Transmission;

public class DialogTransmissionNavigationalAction
{
    /// <summary>
    /// The title of the navigational action.
    /// </summary>
    [JsonPropertyName("title")]
    public ICollection<Localization> Title { get; set; } = [];

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
    public DateTimeOffset? ExpiresAt { get; set; }
}
