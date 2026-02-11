namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Limits;

public sealed class Limits
{
    public EndUserSearchLimits EndUserSearch { get; set; } = new();
    public ServiceOwnerSearchLimits ServiceOwnerSearch { get; set; } = new();
}

public sealed class EndUserSearchLimits
{
    public int MaxPartyFilterValues { get; set; }
    public int MaxServiceResourceFilterValues { get; set; }
    public int MaxOrgFilterValues { get; set; }
    public int MaxExtendedStatusFilterValues { get; set; }
}

public sealed class ServiceOwnerSearchLimits
{
    public int MaxPartyFilterValues { get; set; }
    public int MaxServiceResourceFilterValues { get; set; }
    public int MaxExtendedStatusFilterValues { get; set; }
}
