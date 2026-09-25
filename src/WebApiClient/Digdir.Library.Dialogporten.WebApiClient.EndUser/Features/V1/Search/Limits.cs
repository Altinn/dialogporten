using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

public class Limits
{
    [JsonPropertyName("endUserSearch")]
    public required EndUserSearchLimits EndUserSearch { get; set; }

    [JsonPropertyName("serviceOwnerSearch")]
    public required ServiceOwnerSearchLimits ServiceOwnerSearch { get; set; }
}
