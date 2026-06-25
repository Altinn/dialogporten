using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.ServiceResource;

public class ServiceResourceRole
{
    [JsonPropertyName("urn")]
    public string Urn { get; set; } = null!;

    [JsonPropertyName("name")]
    public ICollection<Localization> Name { get; set; } = [];

    [JsonPropertyName("links")]
    public Links Links { get; set; } = null!;
}
