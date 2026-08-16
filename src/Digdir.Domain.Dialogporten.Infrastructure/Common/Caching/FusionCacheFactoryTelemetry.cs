using System.Diagnostics.Metrics;

namespace Digdir.Domain.Dialogporten.Infrastructure.Common.Caching;

/// <summary>
/// Telemetry surface for <see cref="FusionCacheFactoryRunner"/>. Public so hosts can subscribe the meter
/// (OpenTelemetry only exports meters that are explicitly added via AddMeter).
/// </summary>
public static class FusionCacheFactoryTelemetry
{
    public const string MeterName = "Digdir.Domain.Dialogporten.FusionCacheFactories";

    internal const string CacheNameTag = "cache.name";

    private static readonly Meter Meter = new(MeterName);

    // UpDownCounters rather than observable gauges: no observation callbacks that could retain runner
    // instances or emit duplicate series when multiple runners exist (tests construct their own).
    internal static readonly UpDownCounter<long> ActiveExecutions = Meter.CreateUpDownCounter<long>(
        "dialogporten.fusioncache.factory_executions_active",
        description: "Cache factory executions currently holding an execution permit, including abandoned ones.");

    internal static readonly UpDownCounter<long> ActiveOrphans = Meter.CreateUpDownCounter<long>(
        "dialogporten.fusioncache.factory_orphans_active",
        description: "Cache factory executions abandoned by the runner that have not yet completed.");
}
