using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.IdentifierLookup;

public enum IdentifierLookupGrantType
{
    [System.Runtime.Serialization.EnumMember(Value = @"Role")]
    Role = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"AccessPackage")]
    AccessPackage = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"ResourceDelegation")]
    ResourceDelegation = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"InstanceDelegation")]
    InstanceDelegation = 3,
}

public class IdentifierLookupAuthorizationEvidenceItem
{
    [JsonPropertyName("grantType")]
    [JsonConverter(typeof(JsonStringEnumConverter<IdentifierLookupGrantType>))]
    public IdentifierLookupGrantType GrantType { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = null!;

    [JsonPropertyName("name")]
    public ICollection<Localization> Name { get; set; } = [];

    [JsonPropertyName("links")]
    public Links? Links { get; set; }
}

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
