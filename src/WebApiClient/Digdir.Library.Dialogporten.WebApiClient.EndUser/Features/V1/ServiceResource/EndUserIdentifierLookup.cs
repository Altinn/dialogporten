using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.ServiceResource;

public class EndUserIdentifierLookup
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
    public ICollection<Localization>? Title { get; set; } = [];

    [JsonPropertyName("authorizationEvidence")]
    public IdentifierLookupAuthorizationEvidence AuthorizationEvidence { get; set; } = default!;
}
