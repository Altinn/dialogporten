using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.ServiceResource;

public partial class ServiceResourceMetadata
{
    [JsonPropertyName("serviceResource")]
    public ServiceResource ServiceResource { get; set; } = default!;

    [JsonPropertyName("roles")]
    public ICollection<ServiceResourceRole>? Roles { get; set; }

    [JsonPropertyName("accessPackages")]
    public ICollection<ServiceResourceAccessPackage>? AccessPackages { get; set; }

    [JsonPropertyName("serviceOwner")]
    public ServiceResourceOwner ServiceOwner { get; set; } = default!;
}
