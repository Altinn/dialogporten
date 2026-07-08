using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.ServiceResource;

public class ServiceResource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("isDelegable")]
    public bool IsDelegable { get; set; }

    [JsonPropertyName("minimumAuthenticationLevel")]
    public int MinimumAuthenticationLevel { get; set; }

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; }

    [JsonPropertyName("links")]
    public Links Links { get; set; } = default!;
}
