using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

public class SearchTermResponseItem
{
    /// <summary>The search term (canonical surface form).</summary>
    [JsonPropertyName("w")]
    public required string Word { get; set; }

    /// <summary>The unprefixed service resource identifiers (without "urn:altinn:resource:") the term appears in.</summary>
    [JsonPropertyName("s")]
    public ICollection<string> Resources { get; set; } = [];
}
