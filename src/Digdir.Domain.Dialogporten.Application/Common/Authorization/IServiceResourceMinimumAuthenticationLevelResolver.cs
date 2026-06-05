using Digdir.Domain.Dialogporten.Application.Externals;

namespace Digdir.Domain.Dialogporten.Application.Common.Authorization;

public interface IServiceResourceMinimumAuthenticationLevelResolver
{
    Task<int> GetMinimumAuthenticationLevel(string serviceResource, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, int>> GetMinimumAuthenticationLevels(
        IReadOnlyCollection<string> serviceResources,
        CancellationToken cancellationToken);
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

    private readonly IResourcePolicyInformationRepository _resourcePolicyInformationRepository;

    public ServiceResourceMinimumAuthenticationLevelResolver(
        IResourcePolicyInformationRepository resourcePolicyInformationRepository)
    {
        ArgumentNullException.ThrowIfNull(resourcePolicyInformationRepository);

        _resourcePolicyInformationRepository = resourcePolicyInformationRepository;
    }

    public async Task<int> GetMinimumAuthenticationLevel(string serviceResource, CancellationToken cancellationToken)
    {
        var minimumAuthenticationLevels = await GetMinimumAuthenticationLevels([serviceResource], cancellationToken);
        return minimumAuthenticationLevels.GetValueOrDefault(serviceResource, DefaultMinimumAuthenticationLevel);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetMinimumAuthenticationLevels(
        IReadOnlyCollection<string> serviceResources,
        CancellationToken cancellationToken)
    {
        var requestedResources = serviceResources
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedResources.Count == 0)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        var fetchedMinimumAuthenticationLevels = await _resourcePolicyInformationRepository
            .GetMinimumAuthenticationLevels(cancellationToken);

        var minimumAuthenticationLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var serviceResource in requestedResources)
        {
            if (fetchedMinimumAuthenticationLevels.TryGetValue(serviceResource, out var minimumAuthenticationLevel))
            {
                minimumAuthenticationLevels[serviceResource] = minimumAuthenticationLevel;
                continue;
            }

            // We used to log this as a warning, but as this is hit by the service resource metadata endpoint, the
            // logs might get spammed with lots of warnings if the cache is stale. Currently, the policy information
            // is only updated once every 24 hours so we drop logging here for now.

            minimumAuthenticationLevels[serviceResource] = DefaultMinimumAuthenticationLevel;
        }

        return minimumAuthenticationLevels;
    }
}
