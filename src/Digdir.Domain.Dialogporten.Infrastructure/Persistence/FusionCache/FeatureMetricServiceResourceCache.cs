using System.Collections.Concurrent;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.FusionCache;

/// <summary>
/// FusionCache implementation of IFeatureMetricServiceResourceCache for feature metric service resource caching
/// </summary>
internal sealed class FeatureMetricServiceResourceCache(
    IFusionCacheProvider cacheProvider,
    IDialogDbContext db,
    IResourceRegistry resourceRegistry) : IFeatureMetricServiceResourceCache
{
    private static readonly ConcurrentDictionary<string, TaskCompletionSource> CacheLocks = new();

    private readonly IFusionCache _cache = cacheProvider.GetCache(nameof(IFeatureMetricServiceResourceCache)) ??
                                           throw new ArgumentNullException(nameof(cacheProvider));

    private readonly IDialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly IResourceRegistry _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));

    private const string CacheKeyPrefix = "feature-metric-service-resource:";

    public async Task<ServiceResourceInformation?> GetServiceResource(Guid dialogId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{dialogId}";

        // Try get from cache first
        var serviceResource = await _cache.GetOrDefaultAsync<string?>(cacheKey, token: cancellationToken);
        if (serviceResource is not null)
        {
            return await _resourceRegistry.GetResourceInformation(serviceResource, cancellationToken);
        }

        // Lock to prevent cache stampede
        using var @lock = await Lock(cacheKey, cancellationToken);

        // Check cache again after acquiring lock
        serviceResource = await _cache.GetOrDefaultAsync<string?>(cacheKey, token: cancellationToken);
        if (serviceResource is not null)
        {
            return await _resourceRegistry.GetResourceInformation(serviceResource, cancellationToken);
        }

        serviceResource = await _db.Dialogs
            .Where(x => dialogId == x.Id)
            .Select(x => x.ServiceResource)
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (serviceResource is null) return null;
        await _cache.SetAsync(cacheKey, serviceResource, token: cancellationToken);
        return await _resourceRegistry.GetResourceInformation(serviceResource, cancellationToken);
    }

    private static async Task<IDisposable> Lock(string cacheKey, CancellationToken cancellationToken)
    {
        var mySource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var tokenRegistration = cancellationToken.Register(() => mySource.TrySetCanceled(cancellationToken));

        while (!cancellationToken.IsCancellationRequested)
        {
            var winnerSource = CacheLocks.GetOrAdd(cacheKey, mySource);
            if (ReferenceEquals(winnerSource, mySource))
            {
                return new LockReleaser(cacheKey);
            }

            // What if the winnerSource is canceled or faulted?
            await Task.WhenAny(winnerSource.Task, mySource.Task);
        }

        throw new OperationCanceledException(cancellationToken);
    }

    private sealed class LockReleaser(string cacheKey) : IDisposable
    {
        public void Dispose()
        {
            if (CacheLocks.TryRemove(cacheKey, out var source)) source.TrySetResult();
        }
    }
}
