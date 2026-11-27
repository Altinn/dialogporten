using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Digdir.Domain.Dialogporten.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.ServiceResourceMetadata;

/// <summary>
/// Builds the caller-independent, all-language service-resource metadata catalogue once and caches it as a
/// single low-cardinality entry. Both the public-catalogue query and the authorized-resources query select
/// from this, so the expensive per-resource metadata construction runs once per cache window instead of on
/// every request. The Catalogue builder is slow. So we try to always keep it warm by setting the
/// cache value from the BackgroundService <see cref="ServiceResourceMetadataCatalogueHostedService"/>.
/// This prevents complete eviction, making the first request after cache expiry fast.
/// We dont pass the cancellation token to the factory just to make sure the factory always completes, regardless of
/// user cancellation. In practice, the factory should never run from a request
/// because of the Warmup and Background service.
/// </summary>
internal sealed class ServiceResourceMetadataCatalogue : IServiceResourceMetadataCatalogue
{
    internal const string CacheName = "ServiceResourceMetadataCatalogue";
    private const string CacheKey = "all";

    private readonly IFusionCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public ServiceResourceMetadataCatalogue(
        IFusionCacheProvider cacheProvider,
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        ArgumentNullException.ThrowIfNull(cacheProvider);

        var cache = cacheProvider.GetCache(CacheName);
        ArgumentNullException.ThrowIfNull(cache);

        _cache = cache;
        _scopeFactory = scopeFactory;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public async Task<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>> GetEntries(
        CancellationToken cancellationToken) =>
        await _cache.GetOrSetAsync<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>>(
            CacheKey,
            _ => BuildCatalogue(_hostApplicationLifetime.ApplicationStopping),
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
                Constants.ServiceResourcePrefix + item.ServiceResource.Id,
                item))
            .ToList();
    }
}
