using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.ServiceResource;

public class ServiceResourceMetadataList
{
    [JsonPropertyName("items")]
    public ICollection<ServiceResourceMetadata>? Items { get; set; }
}

public class ServiceResourceMetadata
{
    [JsonPropertyName("serviceResource")]
    public ServiceResource ServiceResource { get; set; } = null!;

    [JsonPropertyName("roles")]
    public ICollection<ServiceResourceRole>? Roles { get; set; }

    [JsonPropertyName("accessPackages")]
    public ICollection<ServiceResourceAccessPackage>? AccessPackages { get; set; }

    [JsonPropertyName("serviceOwner")]
    public ServiceResourceOwner ServiceOwner { get; set; } = null!;
}
