using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using Digdir.Domain.Dialogporten.Infrastructure.ServiceResourceMetadata;

namespace Digdir.Domain.Dialogporten.Infrastructure.Common.Caching;

/// <summary>
/// Execution policy for a cache factory run through <see cref="FusionCacheFactoryRunner"/>. The same policy
/// instance supplies the cache registration's FactoryHardTimeout so the waiter-facing budget and the factory's
/// work ceiling cannot drift apart:
/// FactoryHardTimeout (waiters released / cold misses fail) &lt; CancellationAfter (cooperative cancellation)
/// &lt; AbandonAfter (the wall-clock ceiling: the runner stops awaiting the factory so FusionCache releases the
/// per-key lock). The ordering is pinned by a unit test over <see cref="All"/>.
/// </summary>
internal sealed record FusionCacheFactoryPolicy
{
    public required string CacheName { get; init; }

    /// <summary>FusionCache FactoryHardTimeout: how long cache callers without fail-safe data wait.</summary>
    public required TimeSpan HardTimeout { get; init; }

    /// <summary>When the runner requests cooperative cancellation of the factory.</summary>
    public required TimeSpan CancellationAfter { get; init; }

    /// <summary>
    /// The wall-clock ceiling: when the runner abandons a factory that has not completed, releasing the
    /// FusionCache per-key lock while the orphaned execution keeps its permit until it actually finishes.
    /// </summary>
    public required TimeSpan AbandonAfter { get; init; }

    /// <summary>
    /// Pre-execution permit count: the maximum executions (and thereby DI scopes) that can be live at once
    /// for this cache, orphans included. Bounds the resources abandoned factories can hold; also caps healthy
    /// concurrency, so single-key caches use 1 (FusionCache's per-key lock serializes them anyway) and
    /// high-cardinality caches need headroom for concurrent refreshes of distinct keys.
    /// </summary>
    public required int MaxConcurrentExecutions { get; init; }

    // One small-table EF query; CancellationAfter = 30s Npgsql CommandTimeout + acquisition/mapping headroom.
    public static readonly FusionCacheFactoryPolicy MinimumAuthenticationLevels = new()
    {
        CacheName = ResourcePolicyInformationRepository.MinimumAuthenticationLevelsCacheName,
        HardTimeout = TimeSpan.FromSeconds(5),
        CancellationAfter = TimeSpan.FromSeconds(45),
        AbandonAfter = TimeSpan.FromSeconds(55),
        MaxConcurrentExecutions = 1
    };

    // Single raw SQL query; CancellationAfter = the 60s hard timeout + margin.
    public static readonly FusionCacheFactoryPolicy SubjectResourceReferencedPartyResources = new()
    {
        CacheName = SubjectResourceRepository.ReferencedPartyResourcesCacheName,
        HardTimeout = TimeSpan.FromSeconds(60),
        CancellationAfter = TimeSpan.FromSeconds(90),
        AbandonAfter = TimeSpan.FromSeconds(100),
        MaxConcurrentExecutions = 1
    };

    // Single Dapper query on the singleton NpgsqlDataSource; same shape as the subject-resource query.
    public static readonly FusionCacheFactoryPolicy PartyResourceReferencedResources = new()
    {
        CacheName = PartyResourceRepository.ReferencedResourcesCacheName,
        HardTimeout = TimeSpan.FromSeconds(60),
        CancellationAfter = TimeSpan.FromSeconds(90),
        AbandonAfter = TimeSpan.FromSeconds(100),
        MaxConcurrentExecutions = 1
    };

    // Full SubjectResources table EF read, one command; CancellationAfter = CommandTimeout + headroom.
    public static readonly FusionCacheFactoryPolicy SubjectResources = new()
    {
        CacheName = nameof(SubjectResource),
        HardTimeout = TimeSpan.FromSeconds(5),
        CancellationAfter = TimeSpan.FromSeconds(45),
        AbandonAfter = TimeSpan.FromSeconds(55),
        MaxConcurrentExecutions = 1
    };

    // Multi-step sequential rebuild whose stages can invoke HTTP factories with their own retry budgets, so a
    // theoretical worst case exceeds any sane ceiling. Chosen operational ceiling based on observed rebuild
    // latency; the rebuild must normally finish well within the 120s hard timeout.
    public static readonly FusionCacheFactoryPolicy ServiceResourceMetadataCatalogue = new()
    {
        CacheName = ServiceResourceMetadata.ServiceResourceMetadataCatalogue.CacheName,
        HardTimeout = TimeSpan.FromSeconds(120),
        CancellationAfter = TimeSpan.FromSeconds(150),
        AbandonAfter = TimeSpan.FromSeconds(160),
        MaxConcurrentExecutions = 1
    };

    // Awaits inner authorization caches with 25s hard timeouts, plus union/post-processing margin.
    // High cardinality (one key per caller/filter): permits must cover healthy concurrent refreshes of
    // distinct keys. 16 is an initial, unmeasured limit; capacity rejections (see runner events) are the
    // signal that it is sized wrong.
    public static readonly FusionCacheFactoryPolicy AuthorizedServiceResources = new()
    {
        CacheName = AuthorizedServiceResourcesProvider.CacheName,
        HardTimeout = TimeSpan.FromSeconds(25),
        CancellationAfter = TimeSpan.FromSeconds(40),
        AbandonAfter = TimeSpan.FromSeconds(50),
        MaxConcurrentExecutions = 16
    };

    /// <summary>Every policy in use; the intentional inventory for validation and telemetry.</summary>
    public static readonly IReadOnlyList<FusionCacheFactoryPolicy> All =
    [
        MinimumAuthenticationLevels,
        SubjectResourceReferencedPartyResources,
        PartyResourceReferencedResources,
        SubjectResources,
        ServiceResourceMetadataCatalogue,
        AuthorizedServiceResources
    ];
}
