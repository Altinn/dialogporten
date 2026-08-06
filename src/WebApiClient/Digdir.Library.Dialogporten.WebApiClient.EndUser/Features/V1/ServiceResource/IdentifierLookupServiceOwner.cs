using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.ServiceResource;

public class IdentifierLookupServiceOwner
{
    [JsonPropertyName("orgNumber")]
    public string OrgNumber { get; set; } = null!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = [];
}
