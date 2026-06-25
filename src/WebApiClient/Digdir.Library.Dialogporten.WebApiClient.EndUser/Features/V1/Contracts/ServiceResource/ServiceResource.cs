using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.ServiceResource;

public class ServiceResource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = null!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("isDelegable")]
    public bool IsDelegable { get; set; }

    [JsonPropertyName("minimumAuthenticationLevel")]
    public int MinimumAuthenticationLevel { get; set; }

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; }

    [JsonPropertyName("links")]
    public Links Links { get; set; } = null!;
}