using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Contents;

public class ContentValue
{
    /// <summary>
    /// A list of localizations for the content.
    /// </summary>
    [JsonPropertyName("value")]
    public ICollection<Localization> Value { get; set; } = [];

    /// <summary>
    /// Media type of the content, this can also indicate that the content is embeddable.
    /// </summary>
    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = null!;

    /// <summary>
    /// True if the authenticated user is authorized for this content. If not, the endpoints will
    /// <br/>be replaced with a fixed placeholder. Can be null if not applicable.
    /// <br/>
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool? IsAuthorized { get; set; }
}
