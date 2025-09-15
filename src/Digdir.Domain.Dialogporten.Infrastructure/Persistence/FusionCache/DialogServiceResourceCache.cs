using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.FusionCache;

/// <summary>
/// FusionCache implementation of IDialogServiceResourceCache for dialog service resource caching
/// </summary>
internal sealed class DialogServiceResourceCache(IFusionCacheProvider cacheProvider) : IDialogServiceResourceCache
{
    private readonly IFusionCache _cache = cacheProvider.GetCache(nameof(IDialogServiceResourceCache)) ??
        throw new ArgumentNullException(nameof(cacheProvider));

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
#pragma warning disable CA2016 // Forward the 'cancellationToken' parameter - FusionCache doesn't support cancellation for TryGetAsync
        var result = await _cache.TryGetAsync<string>(key);
#pragma warning restore CA2016
        return result.HasValue ? result.Value : null;
    }

    public async Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken)
    {
        await _cache.SetAsync(key, value, ttl, cancellationToken);
    }
}
