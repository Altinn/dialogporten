using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.ServiceResource;

public class ServiceOwnerIdentifierLookup
{
    [JsonPropertyName("dialogId")]
    public Guid DialogId { get; set; }

    [JsonPropertyName("instanceRef")]
    public required string InstanceRef { get; set; }

    [JsonPropertyName("party")]
    public required string Party { get; set; }

    [JsonPropertyName("serviceResource")]
    public required IdentifierLookupServiceResource ServiceResource { get; set; }

    [JsonPropertyName("serviceOwner")]
    public required IdentifierLookupServiceOwner ServiceOwner { get; set; }

    [JsonPropertyName("title")]
    public ICollection<Localization> Title { get; set; } = [];

    [JsonPropertyName("nonSensitiveTitle")]
    public ICollection<Localization> NonSensitiveTitle { get; set; } = [];
}
