using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;

public class CreateTransmissionNavigationalAction
{
    /// <summary>
    /// The title of the navigational action.
    /// </summary>
    [JsonPropertyName("title")]
    public ICollection<Localization> Title { get; set; } = [];

    /// <summary>
    /// The fully qualified URL of the navigational action.
    /// </summary>
    [JsonPropertyName("url")]
    public required Uri Url { get; set; }

    /// <summary>
    /// The UTC timestamp when the navigational action expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }
}
