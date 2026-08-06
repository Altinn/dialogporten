using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

public class Limits
{
    [JsonPropertyName("endUserSearch")]
    public EndUserSearchLimits EndUserSearch { get; set; } = null!;

    [JsonPropertyName("serviceOwnerSearch")]
    public ServiceOwnerSearchLimits ServiceOwnerSearch { get; set; } = null!;
}
