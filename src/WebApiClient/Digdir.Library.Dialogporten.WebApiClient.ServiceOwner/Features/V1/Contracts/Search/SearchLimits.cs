using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Search;

public class EndUserSearchLimits
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

public class ServiceOwnerSearchLimits
{
    [JsonPropertyName("maxPartyFilterValues")]
    public int MaxPartyFilterValues { get; set; }

    [JsonPropertyName("maxServiceResourceFilterValues")]
    public int MaxServiceResourceFilterValues { get; set; }

    [JsonPropertyName("maxExtendedStatusFilterValues")]
    public int MaxExtendedStatusFilterValues { get; set; }
}

public class Limits
{
    [JsonPropertyName("endUserSearch")]
    public EndUserSearchLimits EndUserSearch { get; set; } = null!;

    [JsonPropertyName("serviceOwnerSearch")]
    public ServiceOwnerSearchLimits ServiceOwnerSearch { get; set; } = null!;
}
