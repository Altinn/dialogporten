using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.FusionCache;

/// <summary>
/// FusionCache implementation of IFeatureMetricServiceResourceCache for dialog service resource caching
/// </summary>
internal sealed class FeatureMetricServiceResourceCache(
    IFusionCacheProvider cacheProvider,
    IDialogDbContext db,
    IResourceRegistry resourceRegistry) : IFeatureMetricServiceResourceCache
{
    private readonly IFusionCache _cache = cacheProvider.GetCache(nameof(IFeatureMetricServiceResourceCache)) ??
        throw new ArgumentNullException(nameof(cacheProvider));
    private readonly IDialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly IResourceRegistry _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));

    private static readonly string CacheKeyPrefix = "feature-metric-service-resource:";

    public async Task<ServiceResourceInformation?> GetServiceResource(Guid dialogId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{dialogId}";

        return await _cache.GetOrSetAsync<ServiceResourceInformation?>(
            cacheKey,
            async (ctx, ct) =>
            {
                var serviceResource = await GetServiceResourceFromDb(dialogId, ct);
                if (serviceResource != null)
                {
                    // Convert to ServiceResourceInformation
                    return await _resourceRegistry.GetResourceInformation(serviceResource, ct);
                }
                else
                {
                    return null;
                }
            },
            token: cancellationToken);
    }

    private async Task<string?> GetServiceResourceFromDb(Guid dialogId, CancellationToken cancellationToken)
    {
        return await _db.Dialogs
            .Where(x => dialogId == x.Id)
            .Select(x => x.ServiceResource)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
