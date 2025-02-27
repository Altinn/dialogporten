using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

internal sealed class IntegrationTestUser : IUser
{
    public IntegrationTestUser(List<Claim> claims)
    {
        var defaultClaims = GetDefaultClaims();
        defaultClaims.AddRange(claims);

        _principal = new ClaimsPrincipal(new ClaimsIdentity(defaultClaims));
    }
    public IntegrationTestUser()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity(GetDefaultClaims()));
    }

    private readonly ClaimsPrincipal _principal;

    public ClaimsPrincipal GetPrincipal() => _principal;

    private static List<Claim> GetDefaultClaims()
    {
        return
        [
            new Claim(ClaimTypes.Name, "Integration Test User"),
            new Claim("acr", Constants.IdportenLoaHigh),
            new Claim(ClaimTypes.NameIdentifier, "integration-test-user"),
            new Claim("pid", "22834498646"),
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
