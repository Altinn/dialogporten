using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Digdir.Domain.Dialogporten.Infrastructure.Common.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.ServiceResourceMetadata;

/// <summary>
/// Builds the caller-independent, all-language service-resource metadata catalogue once and caches it as a
/// single low-cardinality entry. Both the public-catalogue query and the authorized-resources query select
/// from this, so the expensive per-resource metadata construction runs once per cache window instead of on
/// every request. The Catalogue builder is slow. So we separate the build-call from the user-request by populating
/// the cache from the BackgroundService <see cref="ServiceResourceMetadataCatalogueHostedService"/>
/// Advantages:
/// - Even the first request, after cache expiry, is fast.
/// - Request cancellation doesn't affect the cache writes
/// Drawbacks:
/// - Longer mean time to recovery. We mitigate by using a short ErrorInterval and long cache duration.
///   Restart the application to trigger the warmup service for immediate recovery.
/// </summary>
internal sealed class ServiceResourceMetadataCatalogue : IServiceResourceMetadataCatalogue
{
    internal const string CacheName = "ServiceResourceMetadataCatalogue";
    private const string CacheKey = "all";

    private readonly IFusionCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;

    public ServiceResourceMetadataCatalogue(
        IFusionCacheProvider cacheProvider,
        IServiceScopeFactory scopeFactory
    )
    {
        ArgumentNullException.ThrowIfNull(cacheProvider);

        var cache = cacheProvider.GetCache(CacheName);
        ArgumentNullException.ThrowIfNull(cache);

        _cache = cache;
        _scopeFactory = scopeFactory;
    }

    public async Task<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>> GetEntries(
        CancellationToken cancellationToken) =>
        await _cache.GetOrSetAsync<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>>(
            CacheKey,
            BuildCatalogue,
            token: cancellationToken
        );

    public async Task UpdateCatalogueCache(CancellationToken cancellationToken)
    {
        var catalogue = await BuildCatalogue(cancellationToken);
        await _cache.SetAsync(
            CacheKey,
            catalogue,
            token: cancellationToken
        );
    }

    private async Task<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>> BuildCatalogue(CancellationToken cancellationToken)
    {
        // The renewer is a singleton (hosted service), but the item builder and repository depend on the scoped
        // DialogDbContext. Create a scope per refresh so each build gets its own short-lived DbContext.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var itemBuilder = scope.ServiceProvider.GetRequiredService<IServiceResourceMetadataItemBuilder>();
        var partyResourceReferenceRepository = scope.ServiceProvider.GetRequiredService<IPartyResourceReferenceRepository>();

        var referencedResources = await partyResourceReferenceRepository.GetReferencedResources(cancellationToken);

        // acceptedLanguages: null => build the full, all-language items. Per-request language pruning is
        // applied by the query handlers via PrunedCopy, so these cached items are never mutated.
        var items = await itemBuilder.BuildItems(referencedResources, acceptedLanguages: null, cancellationToken);

        return items
            .Select(item => new ServiceResourceMetadataCatalogueEntry(
                Domain.Common.Constants.ServiceResourcePrefix + item.ServiceResource.Id,
                item))
            .ToList();
    }
}
