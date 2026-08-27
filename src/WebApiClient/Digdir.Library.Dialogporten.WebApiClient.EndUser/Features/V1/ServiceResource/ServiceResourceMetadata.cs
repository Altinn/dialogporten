using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.ServiceResource;

public class ServiceResourceMetadata
{
    [JsonPropertyName("serviceResource")]
    public required ServiceResource ServiceResource { get; set; }

    [JsonPropertyName("roles")]
    public ICollection<ServiceResourceRole>? Roles { get; set; } = [];

    [JsonPropertyName("accessPackages")]
    public ICollection<ServiceResourceAccessPackage>? AccessPackages { get; set; } = [];

    [JsonPropertyName("serviceOwner")]
    public required ServiceResourceOwner ServiceOwner { get; set; }
}
