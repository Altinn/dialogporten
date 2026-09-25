using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

public class GetSearchTermsResponse
{
    /// <summary>The resolved language of the returned list (e.g. "nb", "nn", "en").</summary>
    [JsonPropertyName("l")]
    public required string Language { get; set; }

    /// <summary>When the underlying generation run produced this list.</summary>
    [JsonPropertyName("generatedAt")]
    public DateTimeOffset GeneratedAt { get; set; }

    [JsonPropertyName("words")]
    public ICollection<SearchTermResponseItem> Words { get; set; } = [];
}
