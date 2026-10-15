using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.AccessManagement;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using Digdir.Domain.Dialogporten.Infrastructure.ServiceResourceMetadata;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Locking.AsyncKeyed;

namespace Digdir.Domain.Dialogporten.Infrastructure;

public static class IServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void ConfigureCaches()
        {
            // Party/person/org display names from Altinn Name Registry (slow-moving). Keyed per looked-up id.
            services
                .ConfigureFusionCache(nameof(Altinn.NameRegistry), new()
                {
                    Duration = TimeSpan.FromMinutes(1),
                    IsFailSafeEnabled = true,
                    FailSafeMaxDuration = TimeSpan.FromHours(26),
                    FactoryHardTimeout = TimeSpan.FromSeconds(20)
                })
                // Full Altinn service-resource definitions (title/owner/delegable/status/type). Low cardinality, large
                // payload; an input to the service-resource metadata catalogue.
                .ConfigureFusionCache(nameof(Altinn.ResourceRegistry), new()
                {
                    Duration = TimeSpan.FromMinutes(20),
                    FailSafeMaxDuration = TimeSpan.FromHours(26),
                    // The resource list is several megabytes and might take a while to process
                    FactoryHardTimeout = TimeSpan.FromSeconds(10)
                })
                // Service-owner org metadata / short names from Altinn Organization Registry (slow-moving).
                .ConfigureFusionCache(nameof(Altinn.OrganizationRegistry), new()
                {
                    Duration = TimeSpan.FromHours(24),
                    FailSafeMaxDuration = TimeSpan.FromHours(26)
                })
                .ConfigureFusionCache(nameof(Altinn.Authorization), new()
                {
                    // This cache stores PDP authorization results for dialog details.
                    // This will have high cardinality, as each dialog details request is cached separately
                    // per user, dialog instance, party, service resource and Altinn action set.
                    // EU systems fetching details for many dialogs can therefore create hundreds or even
                    // thousands of cache entries within the cache TTL. To avoid memory exhaustion, we
                    // disable the memory cache for PDP authorization results and rely solely on the
                    // distributed cache.
                    SkipMemoryCache = true,
                    // In normal operations, 15 minutes delay is deemed acceptable for authorization data
                    Duration = TimeSpan.FromMinutes(15),
                    // In case Altinn Authorization is down/overloaded, we allow the re-usage of stale authorization data
                    // for an additional 15 minutes. Using default FailSafeThrottleDuration.
                    FailSafeMaxDuration = TimeSpan.FromMinutes(30),
                    // If the request to Altinn Authorization takes too long, we allow the cache to return stale data
                    // temporarily whilst updating the cache in the background. Eager refresh is enabled, and a registered
                    // backplane is used when available.
                    FactorySoftTimeout = TimeSpan.FromSeconds(4),
                    // Timeout for the cache to wait for the factory to complete, which when reached without fail-safe data
                    // will cause an exception to be thrown
                    FactoryHardTimeout = TimeSpan.FromSeconds(25)
                })
                .ConfigureFusionCache(nameof(AuthorizedPartiesResult), new()
                {
                    // This cache stores authorized parties from Altinn Access Management. Dialog search
                    // authorization is based on this data, so search does not use the Altinn.Authorization
                    // PDP cache directly. Cardinality is lower than the dialog details authorization cache,
                    // as entries are keyed by authorized-parties request parameters rather than by each
                    // dialog details authorization check. We therefore allow a memory cache for this.
                    Duration = TimeSpan.FromMinutes(15),
                    // In case Altinn Access Management is down/overloaded, we allow the re-usage of stale authorization data
                    // for an additional 15 minutes. Using default FailSafeThrottleDuration.
                    FailSafeMaxDuration = TimeSpan.FromMinutes(30),
                    // If the request to Altinn Access Management takes too long, we allow the cache to return stale data
                    // temporarily whilst updating the cache in the background. Eager refresh is enabled, and a registered
                    // backplane is used when available.
                    FactorySoftTimeout = TimeSpan.FromSeconds(10),
                    // Timeout for the cache to wait for the factory to complete, which when reached without fail-safe data
                    // will cause an exception to be thrown
                    FactoryHardTimeout = TimeSpan.FromSeconds(25)
                })
                // Subject -> resource (role / access-package) mappings used by dialog-search authorization.
                .ConfigureFusionCache(nameof(SubjectResource), new()
                {
                    Duration = TimeSpan.FromMinutes(20)
                })
                // dialog id -> service-resource lookup for feature-metric tagging; short-lived, no fail-safe.
                .ConfigureFusionCache(nameof(IFeatureMetricServiceResourceCache), new()
                {
                    IsFailSafeEnabled = false,
                    Duration = TimeSpan.FromMinutes(5)
                })
                .ConfigureFusionCache(nameof(IPartyResourceReferenceRepository), new()
                {
                    // High cardinality: one entry per caller party (ps:<hash> keys written by
                    // PartyResourceRepository.GetReferencedResourcesByParty). Keeping these in the in-memory (L1)
                    // tier is unbounded (the shared MemoryCache has no SizeLimit), so working set grows with the
                    // number of distinct parties seen within the cache window. Rely solely on the distributed
                    // (Redis) tier, mirroring the high-cardinality Altinn.Authorization PDP cache above.
                    SkipMemoryCache = true,
                    Duration = TimeSpan.FromMinutes(30),
                    FailSafeMaxDuration = TimeSpan.FromMinutes(60)
                })
                // Single global list ("all") of every Dialogporten-referenced resource urn. Memory-only; the input set
                // for the service-resource metadata catalogue and the includeUnauthorized catalogue path. Distinct from
                // the per-party ps:<hash> cache above (which serves dialog-search pruning fan-out).
                .ConfigureFusionCache(PartyResourceRepository.ReferencedResourcesCacheName, new()
                {
                    Duration = TimeSpan.FromMinutes(30),
                    // A stale value must stay servable for longer than any realistic refresh-failure streak: while one
                    // exists, callers waiting on the per-key memory lock are served it at the factory soft timeout
                    // (behavior pinned by FusionCacheFailSafeSemanticsTests); without one they wait on the lock until
                    // their request is cancelled. Freshness in normal operation is governed by Duration alone.
                    FailSafeMaxDuration = TimeSpan.FromHours(24),
                    // Substantial headroom over the factory's DB work (30s Npgsql CommandTimeout, plus connection
                    // acquisition and result mapping, which the command timeout does not cover). A tight budget
                    // cancels slow-but-succeeding rebuilds, so refreshes never complete and fail-safe silently
                    // becomes the only data source.
                    FactoryHardTimeout = TimeSpan.FromSeconds(60),
                    SkipDistributedCache = true
                })
                // Subjects (roles/access packages) per referenced party-resource; used by the metadata item builder.
                // Invalidated on SubjectResourceRepository.Merge.
                .ConfigureFusionCache(SubjectResourceRepository.ReferencedPartyResourcesCacheName, new()
                {
                    Duration = TimeSpan.FromMinutes(20),
                    // See ReferencedResourcesCacheName above for the rationale behind both values.
                    FailSafeMaxDuration = TimeSpan.FromHours(24),
                    FactoryHardTimeout = TimeSpan.FromSeconds(60)
                })
                .ConfigureFusionCache(ResourcePolicyInformationRepository.MinimumAuthenticationLevelsCacheName, new()
                {
                    Duration = TimeSpan.FromHours(6),
                    // Min auth level is on the authorization hot path; keep stale-but-known data usable well past
                    // Duration so the cache can still serve if ResourcePolicyInformation queries start failing.
                    FailSafeMaxDuration = TimeSpan.FromHours(24)
                })
                .ConfigureFusionCache(AccessManagementMetadataClient.CacheName, new()
                {
                    // Altinn Access Management role + access-package metadata (names, links), assembled and per-language.
                    // Low cardinality (a few global entries); an input to the service-resource metadata catalogue.
                    Duration = TimeSpan.FromHours(1),
                    FailSafeMaxDuration = TimeSpan.FromHours(2)
                })
                .ConfigureFusionCache(ServiceResourceMetadataCatalogue.CacheName, new()
                {
                    // Shared, caller-independent, all-language catalogue of every Dialogporten-referenced service
                    // resource's metadata item (~3k entries). Built once from GetReferencedResources + the item builder
                    // and reused by BOTH the public-catalogue query and the authorized-resources query, so the expensive
                    // per-resource DTO construction no longer runs per request. Single low-cardinality entry -> L1 is
                    // wanted (do not SkipMemoryCache). Memory-only (SkipDistributedCache) to avoid serializing the
                    // several-MB graph to Redis; each replica rebuilds cheaply from its already-cached inputs. Eager
                    // refresh + fail-safe keep the multi-MB rebuild off the request path.
                    Duration = TimeSpan.FromMinutes(20),
                    // A stale value must stay servable for longer than any realistic rebuild-failure streak: while one
                    // exists, callers waiting on the per-key memory lock are served it at FactorySoftTimeout (behavior
                    // pinned by FusionCacheFailSafeSemanticsTests); without one they wait on the lock until their
                    // request is cancelled. Freshness in normal operation is governed by Duration alone.
                    FailSafeMaxDuration = TimeSpan.FromHours(24),
                    EagerRefreshThreshold = 0.8f,
                    // The caller-facing latency ceiling while a rebuild is in flight and a stale value exists: both the
                    // triggering caller and lock waiters fall back to stale when it elapses, and the rebuild completes
                    // in the background. Kept small so callers never wait long on an in-flight rebuild; fresh data
                    // arrives via eager refresh and background completion rather than on the request path.
                    FactorySoftTimeout = TimeSpan.FromSeconds(1),
                    // Substantial headroom for the full sequential rebuild (referenced resources + the item builder's
                    // cached lookups, each DB-bound step capped by the 30s CommandTimeout); sized operationally, not a
                    // guaranteed worst-case bound. Rebuilds that cannot finish within this budget never succeed, and
                    // fail-safe silently becomes the only data source. On a cold miss this hard timeout is the
                    // caller-facing wait ceiling.
                    FactoryHardTimeout = TimeSpan.FromSeconds(120),
                    SkipDistributedCache = true
                })
                .ConfigureFusionCache(AuthorizedServiceResourcesProvider.CacheName, new()
                {
                    // Per-caller (and per party-filter) bounded set of authorized + referenced resource ids backing the
                    // /enduser/serviceresources endpoint and the GraphQL serviceResources query. Replaces a per-request
                    // rebuild of the per-party authorization map + pruning (a multi-second, large query for users with
                    // very many parties). High cardinality (one entry per caller/filter) -> L2-only (SkipMemoryCache),
                    // mirroring Altinn.Authorization and IPartyResourceReferenceRepository; the value is bounded by the
                    // referenced catalogue so it stays tiny even for 25k-party users.
                    // Staleness tradeoff: PartyResourceReferenceCacheInvalidator only expires the lower-level per-party
                    // IPartyResourceReferenceRepository cache on reference changes, NOT this entry. A dialog create/delete
                    // that changes a party's referenced resources is therefore reflected here only after Duration elapses
                    // (up to 15 min). Targeted invalidation is not done because this entry is keyed per authenticated
                    // caller, not per party, so the invalidator (which knows only the party) cannot address it; the
                    // authorized-resource set changes rarely, so the bounded staleness is accepted.
                    SkipMemoryCache = true,
                    FactorySoftTimeout = TimeSpan.FromSeconds(10),
                    // Must be >= the hard timeouts of the caches this factory transitively awaits (AuthorizedPartiesResult
                    // and Altinn.Authorization, both 25s). A smaller value would make this outer factory time out (and, on
                    // a cold entry with no fail-safe data, throw) while the inner authorization call is still legitimately
                    // running within its own larger budget.
                    FactoryHardTimeout = TimeSpan.FromSeconds(25),
                    Duration = TimeSpan.FromMinutes(15),
                    FailSafeMaxDuration = TimeSpan.FromMinutes(30)
                });
        }

        private IServiceCollection ConfigureFusionCache(string cacheName, FusionCacheSettings? settings = null)
        {
            settings ??= new FusionCacheSettings();

            services.AddSingleton(new FusionCacheRegistration(cacheName));
            services.AddFusionCache(cacheName)
                .WithOptions(options => { options.DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(2); })
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = settings.Duration,

                    IsFailSafeEnabled = settings.IsFailSafeEnabled,
                    FailSafeMaxDuration = settings.FailSafeMaxDuration,
                    FailSafeThrottleDuration = settings.FailSafeThrottleDuration,

                    FactorySoftTimeout = settings.FactorySoftTimeout,
                    FactoryHardTimeout = settings.FactoryHardTimeout,

                    DistributedCacheSoftTimeout = settings.DistributedCacheSoftTimeout,
                    DistributedCacheHardTimeout = settings.DistributedCacheHardTimeout,

                    AllowBackgroundDistributedCacheOperations = settings.AllowBackgroundDistributedCacheOperations,

                    JitterMaxDuration = settings.JitterMaxDuration,
                    EagerRefreshThreshold = settings.EagerRefreshThreshold,

                    SkipMemoryCacheWrite = settings.SkipMemoryCache,
                    SkipMemoryCacheRead = settings.SkipMemoryCache,
                    SkipDistributedCacheRead = settings.SkipDistributedCache,
                    SkipDistributedCacheWrite = settings.SkipDistributedCache,

                    // This will stop deserialization exceptions to be re-thrown, which will cause the factory to run as if
                    // the cache entry was not found. This avoids crashes which otherwise would happen if entities that
                    // are cached are changed in a way that makes them incompatible with the cached version.
                    ReThrowSerializationExceptions = false,
                })
                .WithRegisteredSerializer()
                // If Redis is disabled (eg. in local development or non-web runtimes), we must instruct FusionCache to
                // allow the use of InMemoryDistributedCache (it is by default ignored as a IDistributedCache implementation)
                // TryWithRegisteredBackplane is used to ensure that we can continue without Redis as backplane
                .WithRegisteredDistributedCache(ignoreMemoryDistributedCache: false)
                .WithMemoryLocker(new AsyncKeyedMemoryLocker())
                .TryWithRegisteredBackplane();

            return services;
        }
    }

    private sealed class FusionCacheSettings
    {
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan FailSafeMaxDuration { get; set; } = TimeSpan.FromHours(2);
        public TimeSpan FailSafeThrottleDuration { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan FactorySoftTimeout { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan FactoryHardTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan DistributedCacheSoftTimeout { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan DistributedCacheHardTimeout { get; set; } = TimeSpan.FromSeconds(2);
        public bool AllowBackgroundDistributedCacheOperations { get; set; } = true;
        public bool IsFailSafeEnabled { get; set; } = true;
        public TimeSpan JitterMaxDuration { get; set; } = TimeSpan.FromSeconds(2);
        public float EagerRefreshThreshold { get; set; } = 0.8f;
        public bool SkipMemoryCache { get; set; }
        public bool SkipDistributedCache { get; set; }
    }

    public record FusionCacheRegistration(string CacheName);
}
