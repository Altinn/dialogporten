using Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Parties;

internal static class PartiesMapExtensions
{
    public static List<AuthorizedParty> ToAuthorizedParties(this List<AuthorizedPartyDto> source) =>
        source.Select(ToAuthorizedParty).ToList();

    internal static AuthorizedParty ToAuthorizedParty(AuthorizedPartyDto source) => new()
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
        SubParties = source.SubParties?.Select(ToAuthorizedSubParty).ToList() ?? []
    };

    internal static AuthorizedSubParty ToAuthorizedSubParty(AuthorizedPartyDto source) => new()
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
