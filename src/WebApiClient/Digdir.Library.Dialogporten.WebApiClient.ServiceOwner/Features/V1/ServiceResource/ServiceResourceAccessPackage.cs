using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.ServiceResource;

public partial class ServiceResourceAccessPackage
{
    [JsonPropertyName("urn")]
    public string Urn { get; set; } = default!;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; }

    [JsonPropertyName("links")]
    public Links Links { get; set; } = default!;
}
