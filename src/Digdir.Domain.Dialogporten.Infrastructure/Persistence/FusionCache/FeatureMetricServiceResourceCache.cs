using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.FusionCache;

/// <summary>
/// FusionCache implementation of IFeatureMetricServiceResourceCache for feature metric service resource caching
/// </summary>
internal sealed class FeatureMetricServiceResourceCache(
    IFusionCacheProvider cacheProvider,
    IServiceScopeFactory serviceScopeFactory,
    IResourceRegistry resourceRegistry) : IFeatureMetricServiceResourceCache
{
    private readonly IFusionCache _cache = cacheProvider.GetCache(nameof(IFeatureMetricServiceResourceCache)) ??
        throw new ArgumentNullException(nameof(cacheProvider));
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    private readonly IResourceRegistry _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));

    private const string CacheKeyPrefix = "feature-metric-service-resource:";

    public async Task<ServiceResourceInformation?> GetServiceResource(Guid dialogId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{dialogId}";

        var serviceResource = await _cache.GetOrSetAsync<string?>(
            cacheKey,
            (_, ct) => GetServiceResourceFromDb(dialogId, ct),
            token: cancellationToken);

        return serviceResource is not null
            ? await _resourceRegistry.GetResourceInformation(serviceResource, cancellationToken)
            : null;
    }

    private async Task<string?> GetServiceResourceFromDb(Guid dialogId, CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<IDialogDbContext>()
            .Dialogs
            .Where(x => dialogId == x.Id)
            .Select(x => x.ServiceResource)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
