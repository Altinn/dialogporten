using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.ServiceResource;

public class ServiceResourceAccessPackage
{
    [JsonPropertyName("urn")]
    public string Urn { get; set; } = default!;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = [];

    [JsonPropertyName("links")]
    public Links Links { get; set; } = default!;
}
