using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;

namespace Digdir.Domain.Dialogporten.Janitor;

public sealed class ConsoleUser : IUser
{
    public ClaimsPrincipal GetPrincipal()
    {
        var claims = new[]
        {
            new Claim("scope", AuthorizationScope.ServiceOwnerAdminScope),
            new Claim(ClaimsPrincipalExtensions.AltinnOrgClaim, "digdir")
        };
        var identity = new ClaimsIdentity(claims);
        return new ClaimsPrincipal(identity);
    }
}
