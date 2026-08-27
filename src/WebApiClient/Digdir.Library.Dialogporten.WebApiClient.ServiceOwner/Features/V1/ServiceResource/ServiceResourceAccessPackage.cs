using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.ServiceResource;

public class ServiceResourceAccessPackage
{
    [JsonPropertyName("urn")]
    public required string Urn { get; set; }

    [JsonPropertyName("name")]
    public ICollection<Localization> Name { get; set; } = [];

    [JsonPropertyName("links")]
    public required Links Links { get; set; }
}
