using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.ServiceResource;

public class IdentifierLookupServiceOwner
{
    [JsonPropertyName("orgNumber")]
    public required string OrgNumber { get; set; }

    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = [];
}
