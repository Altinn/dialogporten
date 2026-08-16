using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Digdir.Domain.Dialogporten.Infrastructure.Common.Caching;
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
    private readonly FusionCacheFactoryRunner _factoryRunner;

    public ServiceResourceMetadataCatalogue(
        IFusionCacheProvider cacheProvider,
        FusionCacheFactoryRunner factoryRunner)
    {
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(factoryRunner);

        var cache = cacheProvider.GetCache(CacheName);
        ArgumentNullException.ThrowIfNull(cache);

        _cache = cache;
        _factoryRunner = factoryRunner;
    }

    public async Task<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>> GetEntries(CancellationToken cancellationToken) =>
        await _cache.GetOrSetAsync<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>>(
            CacheKey,
            token => _factoryRunner.RunInScope(
                FusionCacheFactoryPolicy.ServiceResourceMetadataCatalogue,
                BuildCatalogue,
                token),
            token: cancellationToken);

    // Runs in the runner's dedicated scope: this cache uses eager refresh, so the build can outlive the
    // request that triggered it (see FusionCacheFactoryRunner). The inner caches this build resolves create
    // their own scopes too, so a nested refresh that detaches from THIS build's scope is safe by construction.
    private static async Task<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>> BuildCatalogue(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var itemBuilder = services.GetRequiredService<IServiceResourceMetadataItemBuilder>();
        var partyResourceReferenceRepository = services.GetRequiredService<IPartyResourceReferenceRepository>();

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
