using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.ServiceResource;

public class ServiceResourceMetadataList
{
    [JsonPropertyName("items")]
    public ICollection<ServiceResourceMetadata>? Items { get; set; } = [];
}
