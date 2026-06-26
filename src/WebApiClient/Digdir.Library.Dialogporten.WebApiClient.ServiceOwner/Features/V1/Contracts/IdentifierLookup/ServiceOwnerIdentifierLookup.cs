using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.IdentifierLookup;

public class ServiceOwnerIdentifierLookup
{
    [JsonPropertyName("dialogId")]
    public Guid DialogId { get; set; }

    [JsonPropertyName("instanceRef")]
    public string InstanceRef { get; set; } = null!;

    [JsonPropertyName("party")]
    public string Party { get; set; } = null!;

    [JsonPropertyName("serviceResource")]
    public IdentifierLookupServiceResource ServiceResource { get; set; } = null!;

    [JsonPropertyName("serviceOwner")]
    public ServiceResourceOwner ServiceOwner { get; set; } = null!;

    [JsonPropertyName("title")]
    public ICollection<Localization>? Title { get; set; }

    [JsonPropertyName("nonSensitiveTitle")]
    public ICollection<Localization>? NonSensitiveTitle { get; set; }
}