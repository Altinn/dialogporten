using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.ServiceResourceMetadata;

/// <summary>
/// Builds the caller-independent, all-language service-resource metadata catalogue once and caches it as a
/// single low-cardinality entry. Both the public-catalogue query and the authorized-resources query select
/// from this, so the expensive per-resource metadata construction runs once per cache window instead of on
/// every request.
/// </summary>
internal sealed class ServiceResourceMetadataCatalogue : IServiceResourceMetadataCatalogue
{
    internal const string CacheName = "ServiceResourceMetadataCatalogue";
    private const string CacheKey = "all";

    private readonly IFusionCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;

    public ServiceResourceMetadataCatalogue(
        IFusionCacheProvider cacheProvider,
        IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(scopeFactory);

        var cache = cacheProvider.GetCache(CacheName);
        ArgumentNullException.ThrowIfNull(cache);

        _cache = cache;
        _scopeFactory = scopeFactory;
    }

    public async Task<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>> GetEntries(CancellationToken cancellationToken) =>
        await _cache.GetOrSetAsync<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>>(
            CacheKey,
            BuildCatalogue,
            token: cancellationToken);

    private async Task<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>> BuildCatalogue(CancellationToken cancellationToken)
    {
        // Build in a fresh DI scope rather than via injected (request-scoped) dependencies. This cache uses
        // eager refresh, so the factory can run on a background task that outlives the request that triggered
        // it; the item builder transitively resolves the request-scoped DialogDbContext (via
        // SubjectResourceRepository), which would already be disposed by then -> ObjectDisposedException. A
        // dedicated scope gives the (possibly background) build its own DbContext for its full lifetime.
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
