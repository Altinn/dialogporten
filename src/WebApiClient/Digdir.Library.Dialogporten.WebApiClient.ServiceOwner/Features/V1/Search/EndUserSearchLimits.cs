using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

public partial class EndUserSearchLimits
{
    [JsonPropertyName("maxPartyFilterValues")]
    public int MaxPartyFilterValues { get; set; }

    [JsonPropertyName("maxServiceResourceFilterValues")]
    public int MaxServiceResourceFilterValues { get; set; }

    [JsonPropertyName("maxOrgFilterValues")]
    public int MaxOrgFilterValues { get; set; }

    [JsonPropertyName("maxExtendedStatusFilterValues")]
    public int MaxExtendedStatusFilterValues { get; set; }
}
