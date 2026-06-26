using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.ServiceResource;

public class ServiceResourceOwner
{
    [JsonPropertyName("orgNumber")]
    public string OrgNumber { get; set; } = null!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; }
}