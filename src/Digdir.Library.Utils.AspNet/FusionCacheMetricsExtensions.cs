using OpenTelemetry.Metrics;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Library.Utils.AspNet;

public static class FusionCacheMetricsExtensions
{
    /// <summary>
    /// Reads that found a value.
    /// </summary>
    /// <remarks>Name: <c>dialogporten_fusioncache_cache_hit_total</c></remarks>
    public const string CacheHit = "fusioncache.cache.hit";

    /// <summary>
    /// Reads that found nothing usable, so the factory had to run (or the default was returned).
    /// </summary>
    /// <remarks>Name: <c>dialogporten_fusioncache_cache_miss_total</c></remarks>
    public const string CacheMiss = "fusioncache.cache.miss";

    /// <summary>
    /// Times the factory threw, e.g. a database or upstream call failed while refreshing a value.
    /// </summary>
    /// <remarks>Name: <c>dialogporten_fusioncache_factory_error_total</c></remarks>
    public const string FactoryError = "fusioncache.factory.error";

    /// <summary>
    /// Times the factory ran past its soft or hard timeout and FusionCache stopped waiting for it.
    /// A slow dependency shows up here.
    /// </summary>
    /// <remarks>Name: <c>dialogporten_fusioncache_factory_synthetic_timeout_total</c></remarks>
    public const string FactorySyntheticTimeout = "fusioncache.factory.synthetic_timeout";

    /// <summary>
    /// Times fail-safe stepped in: getting a fresh value failed, so the expired value was returned
    /// instead of an error.
    /// </summary>
    /// <remarks>Name: <c>dialogporten_fusioncache_failsafe_activate_total</c></remarks>
    public const string FailSafeActivate = "fusioncache.failsafe_activate";

    private static readonly HashSet<string> ExportedInstruments =
    [
        FailSafeActivate,
        FactoryError,
        FactorySyntheticTimeout,
        CacheHit,
        CacheMiss
    ];

    extension(MeterProviderBuilder metrics)
    {
        public MeterProviderBuilder AddFusionCacheMetrics() =>
            metrics
                .AddFusionCacheInstrumentation(options =>
                {
                    options.IncludeMemoryLevel = false;
                    options.IncludeDistributedLevel = false;
                    options.IncludeBackplane = false;
                })
                .AddView(instrument => instrument.Meter.Name == FusionCacheDiagnostics.MeterName &&
                                       !ExportedInstruments.Contains(instrument.Name)
                    ? MetricStreamConfiguration.Drop
                    : null);
    }
}
