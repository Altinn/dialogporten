using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class E2EConstants
{
    public const string TestTokenBaseUrl = "https://altinn-testtools-token-generator.azurewebsites.net/api";
    public const string DefaultServiceOwnerOrgName = "ttd";
    public const string DefaultEndUserSsn = "08844397713";
    public const string AlternateEndUserSsn = "13838599936";

    /// <summary>
    /// Default System user has the following properties:
    /// - Access to AccessPackage "ordinaer-post-til-virksomheten" on <see cref="DefaultServiceResource"/>
    /// - Daglig leder: 29886896598
    /// </summary>
    public static readonly Func<TokenGeneratorEnvironment, string> DefaultSystemUserId = env =>
    {
        return env switch
        {
            TokenGeneratorEnvironment.At23 => "51551dd0-b9c5-4f4b-a4a2-9b7e3669b364",
            TokenGeneratorEnvironment.Tt02 => "7633be4c-dbd5-49f4-bbc4-c058b275de7b",
            TokenGeneratorEnvironment.Yt01 => throw new ArgumentException("No system user in YT"),
            _ => throw new ArgumentException($"System user doesnt exist in env {env}")
        };
    };

    public const string DefaultSystemUserOrgNo = "310057223";
    public const string DefaultSystemUserOrgUrn = "urn:altinn:organization:identifier-no:" + DefaultSystemUserOrgNo;

    /// <summary>
    /// Alternate System user has the following properties:
    /// - Direct access to resource <see cref="AlternateServiceResource"/>
    /// - Daglig leder: 15864799741
    /// </summary>
    public static readonly Func<TokenGeneratorEnvironment, string> AlternateSystemUserId = env =>
    {
        return env switch
        {
            TokenGeneratorEnvironment.At23 => "54cb69df-3d8a-4432-804e-742e40de6211",
            TokenGeneratorEnvironment.Tt02 => "96a46590-1c2a-4e46-a7e7-975f40b985ef",
            TokenGeneratorEnvironment.Yt01 => throw new ArgumentException("No system user in YT"),
            _ => throw new ArgumentException($"System user doesnt exist in env {env}")
        };
    };
    public const string AlternateSystemUserOrgNo = "313006425";
    public const string AlternateSystemUserOrgUrn = "urn:altinn:organization:identifier-no:" + AlternateSystemUserOrgNo;

    private const string DefaultServiceOwnerOrgNr = "991825827";
    private const string Yt01ServiceOwnerOrgNr = "713431400";

    public static string GetDefaultServiceOwnerOrgNr() =>
        Environment.GetDotnetEnvironment() == "yt01"
            ? Yt01ServiceOwnerOrgNr
            : DefaultServiceOwnerOrgNr;

    public const int DefaultTokenTtl = 1800;

    public const string ServiceOwnerScopes =
        AuthorizationScope.ServiceProvider + " " +
        AuthorizationScope.ServiceProviderSearch;

    public const string EndUserScopes = AuthorizationScope.EndUser;

    public const string SystemUserScopes = AuthorizationScope.EndUser;

    public const string EphemeralDialogUrn = "digdir:dialogporten:ephemeral-dialog";

    public const string DefaultServiceResource = "urn:altinn:resource:ttd-dialogporten-automated-tests";
    public const string AlternateServiceResource = "urn:altinn:resource:ttd-dialogporten-automated-tests-2";
    public static readonly string DefaultParty = $"{NorwegianPersonIdentifier.PrefixWithSeparator}{DefaultEndUserSsn}";

    public const string AvailableExternalResource = "urn:altinn:resource:ttd-dialogporten-automated-tests-correspondence";
    public const string UnavailableExternalResource = "urn:altinn:resource:ttd-altinn-events-automated-tests";
    public const string UnavailableSubresource = "someunavailablesubresource";
}
