using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal static class AuthorizedPartiesHelper
{
    public const string PartyTypeSelfIdentified = "SelfIdentified";

    private const string PartyTypeOrganization = "Organization";
    private const string PartyTypePerson = "Person";
    private const string AttributeIdResource = "urn:altinn:resource";
    private const string AttributeIdRoleCode = "urn:altinn:rolecode";
    private const string AttributeIdAccessPackage = "urn:altinn:accesspackage";
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
                result.AuthorizedParties.Add(MapFromDto(authorizedPartyDto, authorizedPartiesRequest));
            }
        }

        return result;
    }

    private static AuthorizedParty MapFromDto(AuthorizedPartiesResultDto dto, AuthorizedPartiesRequest authenticatedUser)
    {
        var party = dto.Type switch
        {
            PartyTypeOrganization => NorwegianOrganizationIdentifier.PrefixWithSeparator + dto.OrganizationNumber,
            PartyTypePerson => NorwegianPersonIdentifier.PrefixWithSeparator + dto.PersonId,
            PartyTypeSelfIdentified => GetSelfIdentifiedPartyUrn(dto),
            _ => throw new ArgumentOutOfRangeException(nameof(dto))
        };

        return new AuthorizedParty
        {
            Party = party,
            PartyUuid = dto.PartyUuid,
            PartyId = dto.PartyId,
            Name = dto.Name,
            DateOfBirth = dto.DateOfBirth,
            PartyType = dto.Type switch
            {
                PartyTypeOrganization => AuthorizedPartyType.Organization,
                PartyTypePerson => AuthorizedPartyType.Person,
                PartyTypeSelfIdentified => AuthorizedPartyType.SelfIdentified,
                _ => throw new ArgumentOutOfRangeException(nameof(dto))
            },
            IsDeleted = dto.IsDeleted,
            HasKeyRole = dto.AuthorizedRoles.Exists(role => KeyRoleCodes.Contains(role)),
            IsCurrentEndUser = dto.Type == PartyTypeSelfIdentified
                ? string.Equals(party, authenticatedUser.PartyIdentifier.FullId, StringComparison.OrdinalIgnoreCase)
                : dto.PersonId == authenticatedUser.PartyIdentifier.Id,
            IsMainAdministrator = dto.AuthorizedRoles.Contains(MainAdministratorRoleCode),
            IsAccessManager = dto.AuthorizedRoles.Contains(AccessManagerRoleCode),
            HasOnlyAccessToSubParties = dto.OnlyHierarchyElementWithNoAccess,
            AuthorizedResources = GetPrefixedResources(dto.AuthorizedResources),
            AuthorizedRolesAndAccessPackages = GetPrefixedRolesAndAccessPackages(dto.AuthorizedRoles, dto.AuthorizedAccessPackages),
            AuthorizedInstances = dto.AuthorizedInstances
                .Select(x => new AuthorizedResource
                {
                    ResourceId = x.ResourceId,
                    InstanceId = x.InstanceId,
                    InstanceRef = x.InstanceRef
                }).ToList(),
            SubParties = dto.Subunits.Count > 0 ? dto.Subunits.Select(x => MapFromDto(x, authenticatedUser)).ToList() : null
        };
    }

    private static string GetSelfIdentifiedPartyUrn(AuthorizedPartiesResultDto dto)
    {
        // Access Management identifies idporten-email users via the emailId field.
        // Other SelfIdentified parties (returned alongside an email user's authorized parties) are legacy
        // self-identified users, addressed by their username (the Name field).
        return dto.EmailId is not null
            ? IdportenEmailUserIdentifier.PrefixWithSeparator + dto.EmailId.ToLowerInvariant()
            : AltinnSelfIdentifiedUserIdentifier.PrefixWithSeparator + dto.Name.ToLowerInvariant();
    }

    private static List<string> GetPrefixedRolesAndAccessPackages(List<string> dtoAuthorizedRoles, List<string> dtoAuthorizedAccessPackages) =>
        dtoAuthorizedRoles.Select(role
                => $"{AttributeIdRoleCode}:{role.ToLowerInvariant()}")
            .Concat(dtoAuthorizedAccessPackages.Select(accessPackage
                => $"{AttributeIdAccessPackage}:{accessPackage.ToLowerInvariant()}")
            ).ToList();

    private static List<string> GetPrefixedResources(List<string> dtoAuthorizedResources) =>
        dtoAuthorizedResources.Select(resource => $"{AttributeIdResource}:{resource}").ToList();
}
