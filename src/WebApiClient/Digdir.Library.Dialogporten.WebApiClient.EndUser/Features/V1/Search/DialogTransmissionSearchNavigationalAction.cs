using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

public class DialogTransmissionSearchNavigationalAction
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
    public required Uri Url { get; set; }

    /// <summary>
    /// The UTC timestamp when the navigational action expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Indicates whether the authenticated user is authorized for this navigational action. If not, the URL will be
    /// <br/>replaced with "urn:dialogporten:unauthorized".
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// A token asserting the authenticated user's authorization for this specific navigational action, as determined
    /// <br/>by its authorization context. Only present when the navigational action has an authorization context and the
    /// <br/>user is authorized. Should be used instead of the dialog token against this action's URL.
    /// </summary>
    [JsonPropertyName("contextToken")]
    public string? ContextToken { get; set; }
}
