using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;

internal static class AuthorizedPartiesResultExtensions
{
    extension(AuthorizedPartiesResult source)
    {
        internal PartiesDto ToDto() =>
            new()
            {
                AuthorizedParties =
                [
                    ..source.AuthorizedParties.Select(x => x.ToDto())
                ]
            };
    }
}

internal static class AuthorizedPartyExtensions
{
    extension(AuthorizedParty authorizedParty)
    {
        internal AuthorizedPartyDto ToDto() =>
            new()
            {
                Party = authorizedParty.Party,
                PartyUuid = authorizedParty.PartyUuid,
                PartyId = authorizedParty.PartyId,
                Name = authorizedParty.Name,
                DateOfBirth = authorizedParty.DateOfBirth,
                PartyType = authorizedParty.PartyType.ToString(),
                IsDeleted = authorizedParty.IsDeleted,
                HasKeyRole = authorizedParty.HasKeyRole,
                IsCurrentEndUser = authorizedParty.IsCurrentEndUser,
                IsMainAdministrator = authorizedParty.IsMainAdministrator,
                IsAccessManager = authorizedParty.IsAccessManager,
                HasOnlyAccessToSubParties = authorizedParty.HasOnlyAccessToSubParties,
                SubParties = authorizedParty.SubParties is null
                    ? null
                    :
                    [
                        ..authorizedParty.SubParties.Select(x => x.ToDto())
                    ]
            };
    }
}
