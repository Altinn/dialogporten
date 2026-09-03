using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.ServiceResource;

public class IdentifierLookupAuthorizationEvidence
{
    [JsonPropertyName("currentAuthenticationLevel")]
    public int CurrentAuthenticationLevel { get; set; }

    [JsonPropertyName("viaRole")]
    public bool ViaRole { get; set; }

    [JsonPropertyName("viaAccessPackage")]
    public bool ViaAccessPackage { get; set; }

    [JsonPropertyName("viaResourceDelegation")]
    public bool ViaResourceDelegation { get; set; }

    [JsonPropertyName("viaInstanceDelegation")]
    public bool ViaInstanceDelegation { get; set; }

    [JsonPropertyName("evidence")]
    public ICollection<IdentifierLookupAuthorizationEvidenceItem> Evidence { get; set; } = [];
}
