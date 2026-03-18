using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class E2EConstants
{
    public const string TestTokenBaseUrl = "https://altinn-testtools-token-generator.azurewebsites.net/api";
    public const string DefaultServiceOwnerOrgNr = "991825827";
    public const string DefaultServiceOwnerOrgName = "ttd";
    public const string DefaultEndUserSsn = "08844397713";
    public const string DefaultSystemUserId = "aaa88f01-d847-2973-9579-76f658b42caa";
    public const string DefaultSystemUserOrgNo = "999888777";

    public const string Yt01ServiceOwnerOrgNr = "713431400";

    public const int DefaultTokenTtl = 1800;

    public const string ServiceOwnerScopes =
        AuthorizationScope.ServiceProvider + " " +
        AuthorizationScope.ServiceProviderSearch;

    public const string EndUserScopes = AuthorizationScope.EndUser;

    public const string SystemUserScopes = AuthorizationScope.EndUser;

    public const string EphemeralDialogUrn = "digdir:dialogporten:ephemeral-dialog";

    public const string DefaultServiceResource = "urn:altinn:resource:ttd-dialogporten-automated-tests";
    public static readonly string DefaultParty = $"{NorwegianPersonIdentifier.PrefixWithSeparator}{DefaultEndUserSsn}";

    public const string AvailableExternalResource = "urn:altinn:resource:ttd-dialogporten-automated-tests-correspondence";
    public const string UnavailableExternalResource = "urn:altinn:resource:ttd-altinn-events-automated-tests";
    public const string UnavailableSubresource = "someunavailablesubresource";
}
