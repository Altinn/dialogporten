using System.Diagnostics;
using System.Security.Claims;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using UserIdType = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogUserType.Values;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    private const string ConsumerClaim = "consumer";
    private const string SupplierClaim = "supplier";
    private const string AuthorityValue = "iso6523-actorid-upis";
    private const char IdDelimiter = ':';
    private const string IdPrefix = "0192";
    private const string AltinnClaimPrefix = "urn:altinn:";
    private const string IdportenAuthLevelClaim = "acr";
    private const string AuthorizationDetailsClaim = "authorization_details";
    private const string AuthorizationDetailsType = "urn:altinn:systemuser";
    private const string AltinnAuthLevelClaim = "urn:altinn:authlevel";
    private const string AltinnUserIdClaim = "urn:altinn:userid";
    private const string AltinnAuthenticationMethod = "urn:altinn:authenticatemethod";
    private const string SelfIdentifiedAuthenticationMethod = "SelfIdentified";
    private const char ScopeClaimSeparator = ' ';
    private const string PidClaim = "pid";
    public const string ScopeClaim = "scope";
    public const string AltinnOrgClaim = "urn:altinn:org";

    public static bool TryGetClaimValue(this ClaimsPrincipal claimsPrincipal, string claimType,
        [NotNullWhen(true)] out string? value)
    {
        value = claimsPrincipal.FindFirst(claimType)?.Value;
        return value is not null;
    }

    public static bool TryGetConsumerOrgNumber(this ClaimsPrincipal claimsPrincipal, [NotNullWhen(true)] out string? orgNumber)
        => claimsPrincipal.FindFirst(ConsumerClaim).TryGetConsumerOrgNumber(out orgNumber);

    public static bool HasScope(this ClaimsPrincipal claimsPrincipal, string scope) =>
        claimsPrincipal.TryGetClaimValue(ScopeClaim, out var scopes) &&
        scopes.Split(ScopeClaimSeparator).Contains(scope);

    public static bool TryGetSupplierOrgNumber(this ClaimsPrincipal claimsPrincipal, [NotNullWhen(true)] out string? orgNumber)
        => claimsPrincipal.FindFirst(SupplierClaim).TryGetConsumerOrgNumber(out orgNumber);

    public static bool TryGetPid(this ClaimsPrincipal claimsPrincipal, [NotNullWhen(true)] out string? pid)
        => claimsPrincipal.FindFirst(PidClaim).TryGetPid(out pid);

    public static bool TryGetOrganizationShortName(this ClaimsPrincipal claimsPrincipal, [NotNullWhen(true)] out string? orgShortName)
    {
        orgShortName = claimsPrincipal.FindFirst(AltinnOrgClaim)?.Value;
        return orgShortName is not null;
    }

    public static bool TryGetPid(this Claim? pidClaim, [NotNullWhen(true)] out string? pid)
    {
        pid = null;
        if (pidClaim is null || pidClaim.Type != PidClaim)
        {
            return false;
        }

        if (NorwegianPersonIdentifier.IsValid(pidClaim.Value))
        {
            pid = pidClaim.Value;
        }

        return pid is not null;
    }

    public static bool TryGetSystemUserId(this Claim claim,
        [NotNullWhen(true)] out string? systemUserId) =>
        new List<Claim> { claim }.TryGetSystemUserId(out systemUserId);

    public static bool TryGetSystemUserId(this List<Claim> claimsList,
        [NotNullWhen(true)] out string? systemUserId) =>
        new ClaimsPrincipal(new ClaimsIdentity(claimsList.ToArray())).TryGetSystemUserId(out systemUserId);

    public static bool TryGetSystemUserId(this ClaimsPrincipal claimsPrincipal,
        [NotNullWhen(true)] out string? systemUserId)
    {
        systemUserId = null;

        if (!claimsPrincipal.TryGetAuthorizationDetailsClaimValue(out var authorizationDetails))
        {
            return false;
        }

        if (authorizationDetails.Length == 0)
        {
            return false;
        }

        var systemUserDetails = authorizationDetails.FirstOrDefault(x => x.Type == AuthorizationDetailsType);

        if (systemUserDetails?.SystemUserIds is null)
        {
            return false;
        }

        systemUserId = systemUserDetails.SystemUserIds.FirstOrDefault();

        return systemUserId is not null;
    }

    public static bool TryGetSystemUserOrgNumber(this ClaimsPrincipal claimsPrincipal,
        [NotNullWhen(true)] out string? orgNumber)
    {
        orgNumber = null;

        if (!claimsPrincipal.TryGetAuthorizationDetailsClaimValue(out var authorizationDetails))
        {
            return false;
        }

        if (authorizationDetails.Length == 0)
        {
            return false;
        }

        var systemUserDetails = authorizationDetails.FirstOrDefault(x => x.Type == AuthorizationDetailsType);

        return systemUserDetails?.SystemUserOrg is not null
               && TryGetOrganizationNumberFromConsumerOrganization(systemUserDetails.SystemUserOrg, out orgNumber);
    }

    public static bool TryGetConsumerOrgNumber(this Claim? consumerClaim, [NotNullWhen(true)] out string? orgNumber)
    {
        orgNumber = null;
        if (consumerClaim is null || consumerClaim.Type != ConsumerClaim)
        {
            return false;
        }

        try
        {
            var consumer = JsonSerializer.Deserialize<ConsumerOrganization>(consumerClaim.Value);
            return TryGetOrganizationNumberFromConsumerOrganization(consumer, out orgNumber);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryGetSelfIdentifiedUserId(this ClaimsPrincipal claimsPrincipal, [NotNullWhen(true)] out string? altinnUserId)
        => claimsPrincipal.TryGetClaimValue(AltinnUserIdClaim, out altinnUserId)
           && claimsPrincipal.TryGetClaimValue(AltinnAuthenticationMethod, out var authMethod)
           && authMethod == SelfIdentifiedAuthenticationMethod;

    public static int GetAuthenticationLevel(this ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal.TryGetClaimValue(AltinnAuthLevelClaim, out var claimValue) && int.TryParse(claimValue, out var level) && level >= 0)
        {
            return level;
        }

        if (claimsPrincipal.TryGetClaimValue(IdportenAuthLevelClaim, out claimValue))
        {
            // The acr claim value is either "idporten-loa-substantial" (previously "Level3") or "idporten-loa-high" (previously "Level4")
            // https://docs.digdir.no/docs/idporten/oidc/oidc_protocol_new_idporten#new-acr-values
            return claimValue switch
            {
                Constants.IdportenLoaSubstantial => 3,
                Constants.IdportenLoaHigh => 4,
                _ => throw new ArgumentException("Unknown acr value")
            };
        }

        if (claimsPrincipal.TryGetSystemUserId(out _))
        {
            // System users are always considered to have authentication level 3
            // See https://github.com/Altinn/altinn-authentication/blob/242ef98de4094f15a1a444aeebb2caa1e808b482/src/Authentication/Controllers/AuthenticationController.cs#L495
            return 3;
        }

        throw new UnreachableException("No authentication level claim found");
    }

    public static IEnumerable<Claim> GetIdentifyingClaims(this IEnumerable<Claim> claims)
    {
        var claimsList = claims.ToList();

        var identifyingClaims = claimsList.Where(c =>
            c.Type == PidClaim ||
            c.Type == ConsumerClaim ||
            c.Type == SupplierClaim ||
            c.Type == IdportenAuthLevelClaim ||
            c.Type.StartsWith(AltinnClaimPrefix, StringComparison.Ordinal)
        ).OrderBy(c => c.Type).ToList();

        // If we have a RAR-claim, this is most likely a system user. Attempt to extract the
        // systemuser-uuid from the authorization_details claim and add to the list.
        var rarClaim = claimsList.FirstOrDefault(c => c.Type == AuthorizationDetailsClaim);
        if (rarClaim != null && rarClaim.TryGetSystemUserId(out var systemUserId))
        {
            identifyingClaims.Add(new Claim(AuthorizationDetailsType, systemUserId));
        }

        return identifyingClaims;
    }

    public static (UserIdType, string externalId) GetUserType(this ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal.TryGetPid(out var externalId))
        {
            return (claimsPrincipal.HasScope(AuthorizationScope.ServiceProvider)
                ? UserIdType.ServiceOwnerOnBehalfOfPerson
                : UserIdType.Person, externalId);
        }

        // https://docs.altinn.studio/authentication/systemauthentication/
        if (claimsPrincipal.TryGetSystemUserId(out externalId))
        {
            return (UserIdType.SystemUser, externalId);
        }

        if (claimsPrincipal.HasScope(AuthorizationScope.ServiceProvider) &&
            claimsPrincipal.TryGetConsumerOrgNumber(out externalId))
        {
            return (UserIdType.ServiceOwner, externalId);
        }

        if (claimsPrincipal.TryGetSelfIdentifiedUserId(out externalId))
        {
            return (UserIdType.SelfIdentifiedUser, externalId);
        }

        return (UserIdType.Unknown, string.Empty);
    }

    public static IPartyIdentifier? GetEndUserPartyIdentifier(this List<Claim> claims)
        => new ClaimsPrincipal(new ClaimsIdentity(claims)).GetEndUserPartyIdentifier();

    public static IPartyIdentifier? GetEndUserPartyIdentifier(this ClaimsPrincipal claimsPrincipal)
    {
        var (userType, externalId) = claimsPrincipal.GetUserType();
        return userType switch
        {
            UserIdType.ServiceOwnerOnBehalfOfPerson or UserIdType.Person
                => NorwegianPersonIdentifier.TryParse(externalId, out var personId)
                    ? personId : null,
            UserIdType.SystemUser
                => SystemUserIdentifier.TryParse(externalId, out var systemUserId)
                    ? systemUserId : null,
            UserIdType.SelfIdentifiedUser
                => AltinnUserIdentifier.TryParse(externalId, out var altinnUserId)
                    ? altinnUserId : null,
            UserIdType.Unknown => null,
            UserIdType.ServiceOwner => null,
            _ => null
        };
    }

    private static bool TryGetAuthorizationDetailsClaimValue(this ClaimsPrincipal claimsPrincipal,
        [NotNullWhen(true)] out SystemUserAuthorizationDetails[]? authorizationDetails)
    {
        authorizationDetails = null;

        if (!claimsPrincipal.TryGetClaimValue(AuthorizationDetailsClaim, out var authDetailsJson))
        {
            return false;
        }

        JsonNode? authDetailsJsonNode;
        try
        {
            authDetailsJsonNode = JsonNode.Parse(authDetailsJson);
            if (authDetailsJsonNode is null)
            {
                return false;
            }
        }
        catch (JsonException)
        {
            // If the JSON is malformed, we cannot parse it, so we return false
            return false;
        }

        // If a claim is an array, but contains only one element, it will be deserialized as a single object by dotnet
        if (authDetailsJsonNode.GetValueKind() is JsonValueKind.Array)
        {
            authorizationDetails = JsonSerializer.Deserialize<SystemUserAuthorizationDetails[]>(authDetailsJson);
        }
        else
        {
            var systemUserAuthorizationDetails = JsonSerializer.Deserialize<SystemUserAuthorizationDetails>(authDetailsJson);
            authorizationDetails = [systemUserAuthorizationDetails!];
        }

        return authorizationDetails is not null;
    }

    private static bool TryGetOrganizationNumberFromConsumerOrganization(ConsumerOrganization? consumerOrganization, [NotNullWhen(true)] out string? orgNumber)
    {
        orgNumber = null;

        if (consumerOrganization is null)
        {
            return false;
        }

        if (!string.Equals(consumerOrganization.Authority, AuthorityValue, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(consumerOrganization.Id))
        {
            return false;
        }

        orgNumber = consumerOrganization.Id.Split(IdDelimiter) switch
        {
            [IdPrefix, var on] => NorwegianOrganizationIdentifier.IsValid(on) ? on : null,
            _ => null
        };

        return orgNumber is not null;
    }

    // https://docs.altinn.studio/authentication/systemauthentication/
    private sealed class SystemUserAuthorizationDetails
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("systemuser_id")]
        public string[]? SystemUserIds { get; set; }

        [JsonPropertyName("systemuser_org")]
        public ConsumerOrganization SystemUserOrg { get; set; } = new();
    }

    private sealed class ConsumerOrganization
    {
        [JsonPropertyName("authority")]
        public string Authority { get; set; } = null!;
        [JsonPropertyName("ID")]
        public string Id { get; set; } = null!;
    }
}
