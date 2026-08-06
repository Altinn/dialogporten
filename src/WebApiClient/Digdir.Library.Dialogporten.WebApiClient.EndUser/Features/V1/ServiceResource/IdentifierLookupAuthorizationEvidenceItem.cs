using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.ServiceResource;

public class IdentifierLookupAuthorizationEvidenceItem
{
    [JsonPropertyName("grantType")]
    [JsonConverter(typeof(JsonStringEnumConverter<IdentifierLookupGrantType>))]
    public IdentifierLookupGrantType GrantType { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = null!;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = [];

    [JsonPropertyName("links")]
    public Links? Links { get; set; }
}
