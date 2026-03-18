using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;

internal static class GetPartiesQueryMapper
{
    internal static PartiesDto ToDto(this AuthorizedPartiesResult source) =>
        new()
        {
            AuthorizedParties = source.AuthorizedParties
                .Select(ToDto)
                .ToList()
        };

    private static AuthorizedPartyDto ToDto(this AuthorizedParty source) =>
        new()
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
            SubParties = source.SubParties?.Select(ToDto).ToList()
        };
}
