using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.ServiceResource;

public class IdentifierLookupServiceOwner
{
    [JsonPropertyName("orgNumber")]
    public string OrgNumber { get; set; } = default!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = default!;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = [];
}
