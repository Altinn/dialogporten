using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;

internal static class PartiesMapper
{
    extension(AuthorizedPartiesResult source)
    {
        public PartiesDto ToDto() => new()
        {
            AuthorizedParties = source.AuthorizedParties
                .Select(party => party.ToDto())
                .ToList()
        };
    }

    extension(AuthorizedParty source)
    {
        public AuthorizedPartyDto ToDto() => new()
        {
            Party = source.Party,
            PartyUuid = source.PartyUuid,
            PartyId = source.PartyId,
            Name = source.Name,
            DateOfBirth = source.DateOfBirth,
            PartyType = source.PartyType.ToString(),
            IsDeleted = source.IsDeleted,
            HasKeyRole = source.HasKeyRole,
            IsCurrentEndUser = source.IsCurrentEndUser,
            IsMainAdministrator = source.IsMainAdministrator,
            IsAccessManager = source.IsAccessManager,
            HasOnlyAccessToSubParties = source.HasOnlyAccessToSubParties,
            SubParties = source.SubParties?
                .Select(subParty => subParty.ToDto())
                .ToList()
        };
    }
}
