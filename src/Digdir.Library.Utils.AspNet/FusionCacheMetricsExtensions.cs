using OpenTelemetry.Metrics;

namespace Digdir.Library.Utils.AspNet;

public static class FusionCacheMetricsExtensions
{
    public const string CoreMeterName = "ZiggyCreatures.Caching.Fusion";

    public const string FailSafeActivate = "fusioncache.failsafe_activate";
    public const string FactoryError = "fusioncache.factory.error";
    public const string FactorySyntheticTimeout = "fusioncache.factory.synthetic_timeout";
    public const string CacheHit = "fusioncache.cache.hit";
    public const string CacheMiss = "fusioncache.cache.miss";

    // Only the signals needed to detect fail-safe silently becoming the data source are exported. Every
    // instrument is tagged with fusioncache.cache.name, and cache.hit additionally with fusioncache.stale,
    // so stale-served share per cache = hit{stale=true} / (hit + miss).
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
        public MeterProviderBuilder AddFusionCacheResilienceMetrics() =>
            metrics
                .AddFusionCacheInstrumentation(options =>
                {
                    options.IncludeMemoryLevel = false;
                    options.IncludeDistributedLevel = false;
                    options.IncludeBackplane = false;
                })
                .AddView(instrument =>
                    instrument.Meter.Name == CoreMeterName && !ExportedInstruments.Contains(instrument.Name)
                        ? MetricStreamConfiguration.Drop
                        : null);
    }
}
