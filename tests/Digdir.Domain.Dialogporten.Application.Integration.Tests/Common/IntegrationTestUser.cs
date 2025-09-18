using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

internal sealed class IntegrationTestUser : IUser
{
    private static string DefaultPid => "22834498646";
    public static string DefaultParty => NorwegianPersonIdentifier.PrefixWithSeparator + DefaultPid;

    public IntegrationTestUser(List<Claim> claims, bool addDefaultClaims = true)
    {
        if (addDefaultClaims)
        {
            claims.AddRange(GetDefaultClaims());
        }

        _principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
    }
    public IntegrationTestUser()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity(GetDefaultClaims()));
    }

    private readonly ClaimsPrincipal _principal;

    public ClaimsPrincipal GetPrincipal() => _principal;

    public static List<Claim> GetDefaultClaims()
    {
        return
        [
            new Claim(ClaimTypes.Name, "Integration Test User"),
            new Claim("acr", Constants.IdportenLoaHigh),
            new Claim(ClaimTypes.NameIdentifier, "integration-test-user"),
            new Claim("pid", DefaultPid),
            new Claim("urn:altinn:org", "ttd"),
            new Claim("consumer",
                """
                {
                    "authority": "iso6523-actorid-upis",
                    "ID": "0192:991825827"
                }
                """)
        ];
    }
}
