using System.Security.Claims;
using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;

namespace Digdir.Domain.Dialogporten.GraphQL.Common;

internal sealed class ApplicationUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public ClaimsPrincipal GetPrincipal()
        => _httpContextAccessor.HttpContext?.User ?? throw new InvalidOperationException("No user principal found");

    public string GetSystemUserOrg()
    {
        var authDetailsString = GetPrincipal().Claims
            .FirstOrDefault(x => x.Type == "authorization_details")?.Value ?? throw new InvalidOperationException("No authorization details found");

        var authorizationDetails = JsonSerializer.Deserialize<AuthorizationDetails>(authDetailsString) ?? throw new InvalidOperationException("No authorization details found");

        var orgParts = authorizationDetails.Org.Id.Split(':', 2);
        return orgParts.Length == 2
            ? authorizationDetails.Org.Id.Split(":")[1]
            : throw new InvalidOperationException($"Error finding OrgNr in {authorizationDetails.Org.Id}");
    }
}
