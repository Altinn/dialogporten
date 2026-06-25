using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.IdentifierLookup;

public class EndUserIdentifierLookup
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
    public IdentifierLookupServiceOwner ServiceOwner { get; set; } = null!;

    [JsonPropertyName("title")]
    public ICollection<Localization>? Title { get; set; }

    [JsonPropertyName("authorizationEvidence")]
    public IdentifierLookupAuthorizationEvidence AuthorizationEvidence { get; set; } = null!;
}