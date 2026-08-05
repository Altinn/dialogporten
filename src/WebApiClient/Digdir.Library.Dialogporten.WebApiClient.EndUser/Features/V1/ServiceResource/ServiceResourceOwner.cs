using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.ServiceResource;

public class ServiceResourceOwner
{
    [JsonPropertyName("orgNumber")]
    public string OrgNumber { get; set; } = default!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = default!;

    [JsonPropertyName("name")]
    public ICollection<Localization>? Name { get; set; } = [];
}
