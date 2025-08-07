using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Service to extract service identifiers from HTTP requests for cost management.
/// Service identifier represents the organization/service making the API call.
/// </summary>
public interface IServiceIdentifierExtractor
{
    /// <summary>
    /// Extracts service identifier (organization number) from the authenticated user's claims
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <returns>Organization number if authenticated as service owner, null otherwise</returns>
    string? ExtractServiceIdentifier(HttpContext httpContext);
}

/// <summary>
/// Implementation of service identifier extractor
/// </summary>
public sealed class ServiceIdentifierExtractor : IServiceIdentifierExtractor
{
    private readonly IUser _user;

    public ServiceIdentifierExtractor(IUser user)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
    }

    public string? ExtractServiceIdentifier(HttpContext httpContext)
    {
        try
        {
            // Use the injected IUser service which handles local development scenarios
            var principal = _user.GetPrincipal();

            // Try to get organization number using the extension method
            if (principal.TryGetOrganizationNumber(out var orgNumber))
            {
                return orgNumber;
            }
            return null;
        }
        catch (Exception)
        {
            // If claim extraction fails, return null (expected for end user operations)
            return null;
        }
    }
}
