using System.Security.Claims;

namespace Digdir.Domain.Dialogporten.Application.Externals.Presentation;

public interface IUser
{
    ClaimsPrincipal GetPrincipal();
}
