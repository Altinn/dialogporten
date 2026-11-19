using Digdir.Library.Utils.AspNet;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests;

public class FusionCacheMetricsExtensionsTests
{
    [Fact]
    public async Task Should_Export_FailSafe_Activation_And_Stale_Hit_Tagged_With_Cache_Name()
    {
        var cacheName = $"failsafe-metrics-{Guid.NewGuid():N}";
        var exporter = new CollectingExporter();
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddFusionCacheResilienceMetrics()
            .AddReader(new BaseExportingMetricReader(exporter))
            .Build();

        await TriggerCacheFailSafe(cacheName);
        meterProvider.ForceFlush();

        var points = exporter.Points.Where(p => p.CacheName == cacheName).ToList();
        var failSafeActivateCount = points
            .Where(p => p.Instrument == FusionCacheMetricsExtensions.FailSafeActivate)
            .Sum(p => p.Value);
        var errorCount = points
            .Where(p => p.Instrument == FusionCacheMetricsExtensions.FactoryError)
            .Sum(p => p.Value);

        Assert.Equal(1, failSafeActivateCount);
        Assert.Equal(1, errorCount);
        Assert.Contains(points, p => p is { Instrument: FusionCacheMetricsExtensions.CacheHit, IsStale: true });
    }

    [Fact]
    public async Task Should_Only_Export_Resilience_Instruments()
    {
        var cacheName = $"failsafe-metrics-{Guid.NewGuid():N}";
        var exporter = new CollectingExporter();
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddFusionCacheResilienceMetrics()
            .AddReader(new BaseExportingMetricReader(exporter))
            .Build();

        await TriggerCacheFailSafe(cacheName);
        meterProvider.ForceFlush();

        string[] allowed =
        [
            FusionCacheMetricsExtensions.FailSafeActivate,
            FusionCacheMetricsExtensions.FactoryError,
            FusionCacheMetricsExtensions.FactorySyntheticTimeout,
            FusionCacheMetricsExtensions.CacheHit,
            FusionCacheMetricsExtensions.CacheMiss
        ];
        Assert.All(exporter.Points, p => Assert.Contains(p.Instrument, allowed));
    }

    private static async Task TriggerCacheFailSafe(string cacheName)
    {
        var ct = TestContext.Current.CancellationToken;
        using var cache = new FusionCache(new FusionCacheOptions { CacheName = cacheName });
        var options = new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMilliseconds(20),
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromHours(1)
        };

        await cache.GetOrSetAsync("key", _ => Task.FromResult(1), options, ct);
        await Task.Delay(TimeSpan.FromMilliseconds(60), ct);
        var failsafe = await cache.GetOrSetAsync<int>("key", _ => throw new InvalidOperationException(), options, ct);
        Assert.Equal(1, failsafe);
    }

    private sealed record ExportedPoint(string Instrument, string? CacheName, bool IsStale, long Value);

    private sealed class CollectingExporter : BaseExporter<Metric>
    {
        public List<ExportedPoint> Points { get; } = [];

        public override ExportResult Export(in Batch<Metric> batch)
        {
            foreach (var metric in batch)
            {
                foreach (ref readonly var point in metric.GetMetricPoints())
                {
                    string? cacheName = null;
                    var isStale = false;
                    foreach (var tag in point.Tags)
                    {
                        if (tag.Key == "fusioncache.cache.name") cacheName = tag.Value as string;
                        if (tag.Key == "fusioncache.stale") isStale = tag.Value is true;
                    }

                    Points.Add(new(metric.Name, cacheName, isStale, point.GetSumLong()));
                }
            }

            return ExportResult.Success;
        }
    }
}
