using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

public class ServiceOwnerSearchLimits
{
    [JsonPropertyName("maxPartyFilterValues")]
    public int MaxPartyFilterValues { get; set; }

    [JsonPropertyName("maxServiceResourceFilterValues")]
    public int MaxServiceResourceFilterValues { get; set; }

    [JsonPropertyName("maxExtendedStatusFilterValues")]
    public int MaxExtendedStatusFilterValues { get; set; }
}
