using Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Parties;

internal static class GraphQlMapper
{
    extension(AuthorizedPartyDto source)
    {
        public AuthorizedParty ToGraphQl() => new()
        {
            Party = source.Party,
            PartyUuid = source.PartyUuid,
            PartyId = source.PartyId,
            Name = source.Name,
            DateOfBirth = source.DateOfBirth,
            PartyType = source.PartyType,
            IsDeleted = source.IsDeleted,
            HasKeyRole = source.HasKeyRole,
            IsCurrentEndUser = source.IsCurrentEndUser,
            IsMainAdministrator = source.IsMainAdministrator,
            IsAccessManager = source.IsAccessManager,
            HasOnlyAccessToSubParties = source.HasOnlyAccessToSubParties,
            SubParties = source.SubParties?.Select(subParty => subParty.ToGraphQlSubParty()).ToList()
        };

        public AuthorizedSubParty ToGraphQlSubParty() => new()
        {
            Party = source.Party,
            PartyUuid = source.PartyUuid,
            PartyId = source.PartyId,
            Name = source.Name,
            DateOfBirth = source.DateOfBirth,
            PartyType = source.PartyType,
            IsDeleted = source.IsDeleted,
            HasKeyRole = source.HasKeyRole,
            IsCurrentEndUser = source.IsCurrentEndUser,
            IsMainAdministrator = source.IsMainAdministrator,
            IsAccessManager = source.IsAccessManager
        };
    }
}
