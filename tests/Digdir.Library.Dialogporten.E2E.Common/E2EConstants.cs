using Digdir.Domain.Dialogporten.Application.Common.Authorization;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class E2EConstants
{
    public const string TestTokenBaseUrl = "https://altinn-testtools-token-generator.azurewebsites.net/api";
    public const string DefaultServiceOwnerOrgNr = "991825827";
    public const string DefaultServiceOwnerOrgName = "ttd";
    public const string DefaultEndUserSsn = "08844397713";

    public const string Yt01ServiceOwnerOrgNr = "713431400";

    public const int DefaultTokenTtl = 1800;

    public const string ServiceOwnerScopes =
        AuthorizationScope.ServiceProvider + " " +
        AuthorizationScope.ServiceProviderSearch;

    public const string EndUserScopes = AuthorizationScope.EndUser;

    public const string EphemeralDialogUrn = "digdir:dialogporten:ephemeral-dialog";
}
