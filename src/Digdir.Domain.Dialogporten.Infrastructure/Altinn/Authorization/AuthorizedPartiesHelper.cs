using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal static class AuthorizedPartiesHelper
{
    private const string PartyTypeOrganization = "Organization";
    private const string PartyTypePerson = "Person";
    private const string AttributeIdResource = "urn:altinn:resource";
    private const string AttributeIdRoleCode = "urn:altinn:rolecode";
    private const string MainAdministratorRoleCode = "HADM";
    private const string AccessManagerRoleCode = "ADMAI";
    private static readonly string[] KeyRoleCodes = ["DAGL", "LEDE", "INNH", "DTPR", "DTSO", "BEST"];
    public static AuthorizedPartiesResult CreateAuthorizedPartiesResult(
        List<AuthorizedPartiesResultDto>? authorizedPartiesDto,
        AuthorizedPartiesRequest authorizedPartiesRequest)
    {
        var result = new AuthorizedPartiesResult();
        if (authorizedPartiesDto is not null)
        {
            foreach (var authorizedPartyDto in authorizedPartiesDto)
            {
                result.AuthorizedParties.Add(MapFromDto(authorizedPartyDto, authorizedPartiesRequest.Value));
            }
        }

        return result;
    }

    private static AuthorizedParty MapFromDto(AuthorizedPartiesResultDto dto, string currentUserValue)
    {
        var party = dto.Type switch
        {
            PartyTypeOrganization => NorwegianOrganizationIdentifier.PrefixWithSeparator + dto.OrganizationNumber,
            PartyTypePerson => NorwegianPersonIdentifier.PrefixWithSeparator + dto.PersonId,
            _ => throw new ArgumentOutOfRangeException(nameof(dto))
        };

        return new AuthorizedParty
        {
            Party = party,
            PartyUuid = dto.PartyUuid,
            Name = dto.Name,
            PartyType = dto.Type switch
            {
                PartyTypeOrganization => AuthorizedPartyType.Organization,
                PartyTypePerson => AuthorizedPartyType.Person,
                _ => throw new ArgumentOutOfRangeException(nameof(dto))
            },
            IsDeleted = dto.IsDeleted,
            HasKeyRole = dto.AuthorizedRoles.Exists(role => KeyRoleCodes.Contains(role)),
            IsCurrentEndUser = dto.PersonId == currentUserValue,
            IsMainAdministrator = dto.AuthorizedRoles.Contains(MainAdministratorRoleCode),
            IsAccessManager = dto.AuthorizedRoles.Contains(AccessManagerRoleCode),
            HasOnlyAccessToSubParties = dto.OnlyHierarchyElementWithNoAccess,
            AuthorizedResources = GetPrefixedResources(dto.AuthorizedResources),
            AuthorizedRoles = GetPrefixedRoles(dto.AuthorizedRoles),
            SubParties = dto.Subunits.Count > 0 ? dto.Subunits.Select(x => MapFromDto(x, currentUserValue)).ToList() : null
        };
    }

    private static List<string> GetPrefixedRoles(List<string> dtoAuthorizedRoles) =>
        dtoAuthorizedRoles.Select(role => $"{AttributeIdRoleCode}:{role.ToLowerInvariant()}").ToList();

    private static List<string> GetPrefixedResources(List<string> dtoAuthorizedResources) =>
        dtoAuthorizedResources.Select(resource => $"{AttributeIdResource}:{resource}").ToList();
}
