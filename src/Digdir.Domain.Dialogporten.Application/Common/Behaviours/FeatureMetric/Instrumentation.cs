using System.Diagnostics.Metrics;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal static class Instrumentation
{
    public static readonly Meter Meter = new("Digdir.Domain.Dialogporten.Application");
}
