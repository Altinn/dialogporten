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
    private static readonly LockPool LockPool = new();

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
        using var @lock = await LockPool.Lock(cacheKey, cancellationToken: cancellationToken);

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
}

/// <summary>
/// Provides asynchronous, keyed locks that allow only one concurrent operation per key.
/// </summary>
/// <remarks>
/// Each unique key represents an independent lock. When a caller requests a lock for a key,
/// they await until the key is free. Once obtained, disposing the returned <see cref="IDisposable"/>
/// releases the lock and allows the next waiter to proceed.
/// </remarks>
internal sealed class LockPool
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _locks = new();

    /// <summary>
    /// Acquires an asynchronous lock associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key that identifies the lock to acquire.</param>
    /// <param name="timeout"></param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the lock request before it is granted.
    /// </param>
    /// <returns>
    /// A task that completes with an <see cref="IDisposable"/> instance.
    /// Disposing this object releases the acquired lock.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via the provided <paramref name="cancellationToken"/>.
    /// </exception>
    public async Task<LockPoolReleaser> Lock(string key, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var mySource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout.HasValue) cts.CancelAfter(timeout.Value);
        await using var tokenRegistration = cts.Token.Register((_, ct) => mySource.TrySetCanceled(ct), null);

        while (!cts.Token.IsCancellationRequested)
        {
            var winnerSource = _locks.GetOrAdd(key, mySource);

            // If we won the race to add, we own the lock
            if (ReferenceEquals(winnerSource, mySource))
            {
                return new LockPoolReleaser(this, key, mySource);
            }

            // Otherwise, wait until the current holder releases the lock, or we get canceled
            await Task.WhenAny(winnerSource.Task, mySource.Task);
        }

        throw new OperationCanceledException(cts.Token);
    }

    /// <summary>
    /// Represents a handle that releases a key lock when disposed.
    /// </summary>
    internal sealed class LockPoolReleaser : IDisposable
    {
        private readonly LockPool _pool;
        private readonly string _key;
        private readonly TaskCompletionSource _mySource;

        /// <summary>
        /// Initializes a new instance of the <see cref="LockPoolReleaser"/> class.
        /// </summary>
        /// <param name="pool">The <see cref="LockPool"/> that created this releaser.</param>
        /// <param name="key">The key associated with the acquired lock.</param>
        /// <param name="mySource"></param>
        public LockPoolReleaser(LockPool pool, string key, TaskCompletionSource mySource)
        {
            _key = key;
            _mySource = mySource;
            _pool = pool;
        }

        /// <summary>
        /// Releases the lock associated with this instance.
        /// </summary>
        public void Dispose()
        {
            if (_pool._locks.TryGetValue(_key, out var source) && ReferenceEquals(source, _mySource))
            {
                _pool._locks.TryRemove(_key, out _);
                _mySource.TrySetResult();
            }
        }
    }
}
