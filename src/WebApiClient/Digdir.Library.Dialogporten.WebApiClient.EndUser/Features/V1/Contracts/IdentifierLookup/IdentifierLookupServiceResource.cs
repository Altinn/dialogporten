using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.IdentifierLookup;

public class IdentifierLookupServiceResource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("isDelegable")]
    public bool IsDelegable { get; set; }

    [JsonPropertyName("minimumAuthenticationLevel")]
    public int MinimumAuthenticationLevel { get; set; }

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; }
}