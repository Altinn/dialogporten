using System.Diagnostics;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

/// <summary>
/// The logger category that the cost-allocation feature metrics are written under. Hosts route these
/// records by this category, so it must match the category of <see cref="LoggingFeatureMetricDeliveryContext"/>.
/// </summary>
public static class FeatureMetricLogCategory
{
    public static readonly string Name =
        typeof(LoggingFeatureMetricDeliveryContext).FullName ?? throw new UnreachableException();
}
