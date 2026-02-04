using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using System.Security.Claims;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

internal sealed class ApplicationUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public ClaimsPrincipal GetPrincipal()
        => _httpContextAccessor.HttpContext?.User ??
            throw new InvalidOperationException("No user principal found");
}
