using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using System.Security.Claims;

namespace Digdir.Domain.Dialogporten.Service;

internal sealed class ServiceUser : IUser
{
    public ClaimsPrincipal GetPrincipal()
    {
        throw new NotSupportedException(
            "At the time of this writing, Digdir.Domain.Dialogporten.Service should not " +
            "be using application commands or queries requiring the need for a user.");
    }
}
