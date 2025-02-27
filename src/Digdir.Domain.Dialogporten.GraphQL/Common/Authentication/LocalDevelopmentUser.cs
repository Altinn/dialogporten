using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;

namespace Digdir.Domain.Dialogporten.GraphQL.Common.Authentication;

internal sealed class LocalDevelopmentUser : IUser
{
    private readonly ClaimsPrincipal _principal = new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Name, "Local Development User"),
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
}
