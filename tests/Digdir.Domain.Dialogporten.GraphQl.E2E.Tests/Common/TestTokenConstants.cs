using Digdir.Domain.Dialogporten.Application.Common.Authorization;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;

public static class TestTokenConstants
{
    public const string TestTokenBaseUrl = "https://altinn-testtools-token-generator.azurewebsites.net/api";
    public const string DefaultServiceOwnerOrgNr = "991825827";
    public const string DefaultServiceOwnerOrgName = "ttd";
    public const string DefaultEndUserSsn = "08844397713";

    public const string Yt01ServiceOwnerOrgNr = "713431400";

    public const int DefaultTokenTtl = 600;

    public const string ServiceOwnerScopes =
        AuthorizationScope.ServiceProvider + " " +
        AuthorizationScope.ServiceProviderSearch;

    public const string EndUserScopes = AuthorizationScope.EndUser;
}
