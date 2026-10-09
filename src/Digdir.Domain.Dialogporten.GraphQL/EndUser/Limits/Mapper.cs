using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.Limits.Queries.Get;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Limits;

internal static class LimitsMapExtensions
{
    public static Limits ToLimits(this GetLimitsDto source) => new()
    {
        EndUserSearch = ToEndUserSearchLimits(source.EndUserSearch),
        ServiceOwnerSearch = ToServiceOwnerSearchLimits(source.ServiceOwnerSearch)
    };

    private static EndUserSearchLimits ToEndUserSearchLimits(EndUserSearchLimitsDto source) => new()
    {
        MaxPartyFilterValues = source.MaxPartyFilterValues,
        MaxServiceResourceFilterValues = source.MaxServiceResourceFilterValues,
        MaxOrgFilterValues = source.MaxOrgFilterValues,
        MaxExtendedStatusFilterValues = source.MaxExtendedStatusFilterValues
    };

    private static ServiceOwnerSearchLimits ToServiceOwnerSearchLimits(ServiceOwnerSearchLimitsDto source) => new()
    {
        MaxPartyFilterValues = source.MaxPartyFilterValues,
        MaxServiceResourceFilterValues = source.MaxServiceResourceFilterValues,
        MaxExtendedStatusFilterValues = source.MaxExtendedStatusFilterValues
    };
}
