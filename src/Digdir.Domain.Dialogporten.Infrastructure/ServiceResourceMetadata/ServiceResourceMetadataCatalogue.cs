using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Domain.Common;
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
    private const string CacheKeyKnownLanguages = "sr-catalogue-known-languages";
    private const string CacheKeyCatalogue = "sr-catalogue";

    private static readonly Func<List<AcceptedLanguage>, string> CacheKeyCatalogueByLang =
        lng => $"sr-catalogue-by-lang-{ToCacheString(lng)}";

    private static string ToCacheString(List<AcceptedLanguage> lng) =>
        string.Join(',', lng.OrderByDescending(x => x.Weight).Select(x => x.LanguageCode));


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

    public async Task<IReadOnlyList<ServiceResourceMetadataItemDto>> GetCatalogueDtos(
        List<AcceptedLanguage>? languages,
        CancellationToken cancellationToken
    )
    {
        if (languages == null)
        {
            return await _cache.GetOrSetAsync<IReadOnlyList<ServiceResourceMetadataItemDto>>(
                CacheKeyCatalogue,
                (_, ct) => BuildCatalogue(languages, ct),
                token: cancellationToken);
        }

        var knownLanguages = await GetKnownLanguages(cancellationToken);
        languages = languages
            .Where(l => knownLanguages.Contains(l.LanguageCode))
            .OrderByDescending(l => l.Weight)
            .Take(2) // We ignore more than 2 languages to limit cardinality in the cache
            .ToList();

        return await _cache.GetOrSetAsync<IReadOnlyList<ServiceResourceMetadataItemDto>>(
                CacheKeyCatalogueByLang(languages),
                (_, ct) => BuildCatalogue(languages, ct),
                token: cancellationToken);
    }

    private async Task<HashSet<string>> GetKnownLanguages(CancellationToken ct)
    {
        return await _cache.GetOrSetAsync<HashSet<string>>(
            CacheKeyKnownLanguages,
            async (_, cancellationToken) =>
            {
                var dtos = await GetCatalogueDtos(null, cancellationToken);
                return dtos
                    .SelectMany(d => d.ServiceResource.Name
                        .Concat(d.ServiceOwner.Name)
                        .Concat(d.AccessPackages.SelectMany(a => a.Name))
                        .Concat(d.Roles.SelectMany(r => r.Name))
                        .Select(x => x.LanguageCode))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            },
            token: ct);
    }

    private async Task<IReadOnlyList<ServiceResourceMetadataItemDto>> BuildCatalogue(
        List<AcceptedLanguage>? languages,
        CancellationToken cancellationToken
    )
    {
        // Build in a fresh DI scope rather than via injected (request-scoped) dependencies. This cache uses
        // eager refresh, so the factory can run on a background task that outlives the request that triggered
        // it; the item builder transitively resolves the request-scoped DialogDbContext (via
        // SubjectResourceRepository), which would already be disposed by then -> ObjectDisposedException. A
        // dedicated scope gives the (possibly background) build its own DbContext for its full lifetime.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var itemBuilder = scope.ServiceProvider.GetRequiredService<IServiceResourceMetadataItemBuilder>();
        var partyResourceReferenceRepository = scope.ServiceProvider
            .GetRequiredService<IPartyResourceReferenceRepository>();

        var referencedResources = await partyResourceReferenceRepository.GetReferencedResources(cancellationToken);

        // acceptedLanguages: null => build the full, all-language items. Per-request language pruning is
        // applied by the query handlers via PrunedCopy, so these cached items are never mutated.
        var items = await itemBuilder.BuildItems(referencedResources, acceptedLanguages: null, cancellationToken);

        return items
            .Select(item => new ServiceResourceMetadataCatalogueEntry(
                Constants.ServiceResourcePrefix + item.ServiceResource.Id,
                item)
            )
            .ToSortedPrunedItems(languages)
            .ToList();
    }
}
