using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using AuthConstants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

internal sealed class LocalDevelopmentUser : IUser
{
    private readonly ClaimsPrincipal _principal = new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Name, "Local Development User"),
        new Claim("acr", AuthConstants.IdportenLoaHigh),
        new Claim(ClaimTypes.NameIdentifier, "local-development-user"),
        new Claim("pid", "03886595947"),
        new Claim("scope", string.Join(" ", AuthorizationScope.AllScopes.Value)),
        new Claim("consumer",
            """
            {
                "authority": "iso6523-actorid-upis",
                "ID": "0192:991825827"
            }
            """)
    ]));

    public ClaimsPrincipal GetPrincipal() => _principal;
    public string GetSystemUserOrg() => throw new NotImplementedException();
}
