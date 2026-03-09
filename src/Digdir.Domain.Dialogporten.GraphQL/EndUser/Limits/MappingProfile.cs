using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.Limits.Queries.Get;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Limits;

internal static class GraphQlMapper
{
    extension(GetLimitsDto source)
    {
        public Limits ToGraphQl() => new()
        {
            EndUserSearch = source.EndUserSearch.ToGraphQl(),
            ServiceOwnerSearch = source.ServiceOwnerSearch.ToGraphQl()
        };
    }

    extension(EndUserSearchLimitsDto source)
    {
        public EndUserSearchLimits ToGraphQl() => new()
        {
            MaxPartyFilterValues = source.MaxPartyFilterValues,
            MaxServiceResourceFilterValues = source.MaxServiceResourceFilterValues,
            MaxOrgFilterValues = source.MaxOrgFilterValues,
            MaxExtendedStatusFilterValues = source.MaxExtendedStatusFilterValues
        };
    }

    extension(ServiceOwnerSearchLimitsDto source)
    {
        public ServiceOwnerSearchLimits ToGraphQl() => new()
        {
            MaxPartyFilterValues = source.MaxPartyFilterValues,
            MaxServiceResourceFilterValues = source.MaxServiceResourceFilterValues,
            MaxExtendedStatusFilterValues = source.MaxExtendedStatusFilterValues
        };
    }
}
