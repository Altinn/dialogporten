using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Infrastructure.Common.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal sealed class AuthorizedServiceResourcesProvider : IAuthorizedServiceResourcesProvider
{
    internal const string CacheName = "AuthorizedServiceResources";

    // Threshold passed to GetAuthorizedResourcesForSearch to force pruning regardless of result size. Unlike
    // dialog search (where pruning is a size-gated optimization), the result here IS the resource list and must
    // be a subset of the Dialogporten-referenced catalogue, so pruning must always run.
    private const int ForceReferencedCatalogueSubsetPruning = 0;

    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    // The one deliberately request-scoped constructor dependency: the principal is resolved from it on the
    // request thread before any cache access (see GetAuthorizedServiceResources), so it never crosses into
    // the detached factory. Everything the factory needs comes from the runner's scope instead.
    private readonly IUser _user;
    private readonly IFusionCache _cache;
    private readonly FusionCacheFactoryRunner _factoryRunner;

    public AuthorizedServiceResourcesProvider(
        IUser user,
        IFusionCacheProvider cacheProvider,
        FusionCacheFactoryRunner factoryRunner)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(factoryRunner);

        var cache = cacheProvider.GetCache(CacheName);
        ArgumentNullException.ThrowIfNull(cache);

        _user = user;
        _cache = cache;
        _factoryRunner = factoryRunner;
    }

    public async Task<AuthorizedServiceResources> GetAuthorizedServiceResources(
        string[]? partyFilter,
        CancellationToken cancellationToken)
    {
        // Resolve the principal on the request thread (throws if it is not an end user) so unauthorized principals
        // fail before any cache access and are never cached.
        var principal = _user.GetPrincipal();
        var partyIdentifier = principal.GetEndUserPartyIdentifierOrThrow();

        // Normalize the optional party filter so semantically-equal filters share a cache entry.
        var normalizedFilter = NormalizeFilter(partyFilter);

        // Cache the result per (caller, filter). For the common case the value is the bounded, already-pruned
        // union (bounded by the referenced catalogue). For callers with very many parties on an unfiltered
        // request it is the tiny "return the full catalogue" signal (the expensive per-party pruning is skipped).
        return await _cache.GetOrSetAsync<AuthorizedServiceResources>(
            GetCacheKey(partyIdentifier.FullId, normalizedFilter),
            token => _factoryRunner.RunInScope(
                FusionCacheFactoryPolicy.AuthorizedServiceResources,
                async (services, ct) =>
                {
                    // The factory runs detached from the request (eager refresh / soft-timeout background
                    // completion), where HttpContext — and thus IUser.GetPrincipal() — is no longer available.
                    // The runner's fresh scope gives the resolution live dependencies; the ambient principal
                    // gives its IUser the request-thread principal. Established BEFORE resolving scoped
                    // services and disposed only after the resolution has been awaited.
                    using var _ = AmbientUserPrincipal.Use(principal);
                    return await ResolveAuthorizedServiceResources(services, normalizedFilter, ct);
                },
                token),
            token: cancellationToken);
    }

    // Static and resolving its own dependencies: the factory can run detached from the request (see
    // FusionCacheFactoryRunner), so it must not capture request-scoped services from this instance.
    private static async Task<AuthorizedServiceResources> ResolveAuthorizedServiceResources(
        IServiceProvider services,
        string[]? normalizedFilter,
        CancellationToken cancellationToken)
    {
        // Safety bound: on an unfiltered request, if the caller is authorized to more parties than the configured
        // limit, return the full referenced catalogue and skip the expensive per-party resolution entirely. The
        // count uses the lightweight authorized-parties lookup (no resource/subject resolution, no pruning), so
        // we never do the heavy GetAuthorizedResourcesForSearch work just to discover the caller is over the limit.
        var limits = services.GetRequiredService<IOptionsSnapshot<ApplicationSettings>>().Value.Limits;
        var maxPartiesBeforeFullCatalogue = limits.AuthorizedServiceResources.MaxAuthorizedPartiesBeforeFullCatalogue;

        // The fallback must trip no later than the per-party caching threshold: above that threshold the per-party
        // pruning query still runs but its results are not written to the per-party cache, so a caller between the
        // two limits would otherwise run the uncached multi-party query this fallback exists to prevent. The two
        // limits are independent settings, so reconcile them here by clamping the fallback down to the (lower)
        // caching threshold when both are enabled. A 0 (disabled) fallback stays disabled.
        var cachingThreshold = limits.PartyResourcePruning.MaxPartiesCachingThreshold;
        if (maxPartiesBeforeFullCatalogue > 0 && cachingThreshold > 0 && cachingThreshold < maxPartiesBeforeFullCatalogue)
        {
            maxPartiesBeforeFullCatalogue = cachingThreshold;
        }

        if (normalizedFilter is null && maxPartiesBeforeFullCatalogue > 0)
        {
            var userParties = await services.GetRequiredService<IUserParties>().GetUserParties(cancellationToken);
            if (userParties.FlattenedCount() > maxPartiesBeforeFullCatalogue)
            {
                return new AuthorizedServiceResources(IncludeFullCatalogue: true, ResourceUrns: []);
            }
        }

        // Push the party filter down as constraint parties so only the requested parties are resolved and pruned,
        // instead of resolving every authorized party and filtering in memory. The filter is capped upstream
        // (MaxPartyFilterValues), so this keeps a filtered request cheap and bounded even for callers authorized
        // to very many parties (which otherwise resolves all parties and hits the factory timeout). For an
        // unfiltered request the constraint is empty (and the party count is already bounded by the fallback).
        // The Access Management call is cached one level down (AuthorizedParties cache, keyed per filter).
        // DialogIds are not needed for this endpoint, so skip resolving them. Pruning happens once inside the
        // search call (ForceReferencedCatalogueSubsetPruning), so the provider does not prune again.
        var constraintParties = normalizedFilter is null ? [] : normalizedFilter.ToList();
        var result = await services.GetRequiredService<IAltinnAuthorization>().GetAuthorizedResourcesForSearch(
            constraintParties, [], includeDialogIds: false,
            minResourcesPruningThreshold: ForceReferencedCatalogueSubsetPruning, cancellationToken: cancellationToken);

        // Union across the resolved parties. The party filter is pushed down as constraint parties, and every
        // IAltinnAuthorization implementation honors it — production AltinnAuthorizationClient filters
        // relevantParties, LocalDevelopmentAltinnAuthorization filters its dialog query, and the test fakes
        // filter their configured result — so result.ResourcesByParties is already restricted to the requested
        // parties and the union below is the final authorized set.
        var authorizedResources = new HashSet<string>(Comparer);
        foreach (var resources in result.ResourcesByParties.Values)
        {
            authorizedResources.UnionWith(resources);
        }

        return new AuthorizedServiceResources(IncludeFullCatalogue: false, ResourceUrns: [.. authorizedResources]);
    }

    private static string[]? NormalizeFilter(string[]? partyFilter)
    {
        // Collapsing an all-blank filter to null (i.e. treating it as "no filter") is only safe because
        // SearchAuthorizedServiceResourcesQueryValidator rejects blank/invalid party values upstream, so a
        // supplied filter can never silently degrade into an unfiltered (full-catalogue) request here.
        if (partyFilter is not { Length: > 0 })
        {
            return null;
        }

        // Order so semantically-equal filters produce the same cache key.
        var normalized = partyFilter
            .NormalizeParties(Comparer)
            .OrderBy(x => x, Comparer)
            .ToArray();

        return normalized.Length > 0 ? normalized : null;
    }

    private static string GetCacheKey(string fullPartyId, string[]? normalizedFilter)
    {
        var filterPart = normalizedFilter is null
            ? "*"
            : string.Join(',', normalizedFilter).ToLowerInvariant();
        return CacheKeyHash.Build("asr:", $"{fullPartyId.ToLowerInvariant()}|{filterPart}");
    }
}
