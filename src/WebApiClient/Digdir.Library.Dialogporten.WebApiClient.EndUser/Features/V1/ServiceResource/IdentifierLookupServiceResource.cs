using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.ServiceResource;

public class IdentifierLookupServiceResource
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("isDelegable")]
    public bool IsDelegable { get; set; }

    [JsonPropertyName("minimumAuthenticationLevel")]
    public int MinimumAuthenticationLevel { get; set; }

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = [];
}
