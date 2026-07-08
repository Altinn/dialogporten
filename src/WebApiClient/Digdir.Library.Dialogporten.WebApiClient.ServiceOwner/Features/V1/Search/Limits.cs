using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

public partial class Limits
{
    [JsonPropertyName("endUserSearch")]
    public EndUserSearchLimits EndUserSearch { get; set; } = default!;

    [JsonPropertyName("serviceOwnerSearch")]
    public ServiceOwnerSearchLimits ServiceOwnerSearch { get; set; } = default!;
}
