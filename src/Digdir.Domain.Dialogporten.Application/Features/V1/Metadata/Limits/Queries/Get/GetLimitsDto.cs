namespace Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.Limits.Queries.Get;

public sealed class GetLimitsDto
{
    public EndUserSearchLimitsDto EndUserSearch { get; set; } = new();
    public ServiceOwnerSearchLimitsDto ServiceOwnerSearch { get; set; } = new();
}

public sealed class EndUserSearchLimitsDto
{
    public int MaxPartyFilterValues { get; set; }
    public int MaxServiceResourceFilterValues { get; set; }
    public int MaxOrgFilterValues { get; set; }
    public int MaxExtendedStatusFilterValues { get; set; }
}

public sealed class ServiceOwnerSearchLimitsDto
{
    public int MaxPartyFilterValues { get; set; }
    public int MaxServiceResourceFilterValues { get; set; }
    public int MaxExtendedStatusFilterValues { get; set; }
}
