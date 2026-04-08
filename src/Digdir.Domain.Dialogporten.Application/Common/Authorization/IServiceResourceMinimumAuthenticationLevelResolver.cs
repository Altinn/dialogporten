using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Common.Authorization;

public interface IServiceResourceMinimumAuthenticationLevelResolver
{
    Task<int> GetMinimumAuthenticationLevel(string serviceResource, CancellationToken cancellationToken);
}

internal sealed class ServiceResourceMinimumAuthenticationLevelResolver : IServiceResourceMinimumAuthenticationLevelResolver
{
    /// <summary>
    /// The default minimum authentication level applied when no resource policy information is found for a given
    /// service resource. Level 3 corresponds to "idporten-loa-substantial" and is the baseline required by most
    /// Norwegian public-sector services. Falling back to this value rather than denying access outright avoids
    /// incorrect authorization failures when the resource policy sync has not yet run (e.g., on first deploy or
    /// during transient sync delays), while still ensuring that low-assurance users (level &lt; 3) are rejected.
    /// </summary>
    private const int DefaultMinimumAuthenticationLevel = 3;

    private readonly IDialogDbContext _db;
    private readonly ILogger<ServiceResourceMinimumAuthenticationLevelResolver> _logger;

    public ServiceResourceMinimumAuthenticationLevelResolver(
        IDialogDbContext db,
        ILogger<ServiceResourceMinimumAuthenticationLevelResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);

        _db = db;
        _logger = logger;
    }

    public async Task<int> GetMinimumAuthenticationLevel(string serviceResource, CancellationToken cancellationToken)
    {
        var minimumAuthenticationLevel = await _db.ResourcePolicyInformation
            .AsNoTracking()
            .Where(x => x.Resource == serviceResource)
            .Select(x => (int?)x.MinimumAuthenticationLevel)
            .FirstOrDefaultAsync(cancellationToken);

        if (minimumAuthenticationLevel is not null)
        {
            return minimumAuthenticationLevel.Value;
        }

        _logger.LogWarning(
            "Could not find resource policy information for resource {ServiceResource}, " +
            "falling back to default minimum authentication level {DefaultMinimumAuthenticationLevel}",
            serviceResource,
            DefaultMinimumAuthenticationLevel);

        return DefaultMinimumAuthenticationLevel;
    }
}
