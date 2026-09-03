using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

public class Limits
{
    [JsonPropertyName("endUserSearch")]
    public required EndUserSearchLimits EndUserSearch { get; set; }

    [JsonPropertyName("serviceOwnerSearch")]
    public required ServiceOwnerSearchLimits ServiceOwnerSearch { get; set; }
}
