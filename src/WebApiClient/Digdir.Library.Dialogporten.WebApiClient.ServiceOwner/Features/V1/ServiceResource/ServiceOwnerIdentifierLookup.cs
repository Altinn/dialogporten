using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.ServiceResource;

public class ServiceOwnerIdentifierLookup
{
    [JsonPropertyName("dialogId")]
    public Guid DialogId { get; set; }

    [JsonPropertyName("instanceRef")]
    public string InstanceRef { get; set; } = default!;

    [JsonPropertyName("party")]
    public string Party { get; set; } = default!;

    [JsonPropertyName("serviceResource")]
    public IdentifierLookupServiceResource ServiceResource { get; set; } = default!;

    [JsonPropertyName("serviceOwner")]
    public IdentifierLookupServiceOwner ServiceOwner { get; set; } = default!;

    [JsonPropertyName("title")]
    public ICollection<Localization>? Title { get; set; }

    [JsonPropertyName("nonSensitiveTitle")]
    public ICollection<Localization>? NonSensitiveTitle { get; set; }
}
