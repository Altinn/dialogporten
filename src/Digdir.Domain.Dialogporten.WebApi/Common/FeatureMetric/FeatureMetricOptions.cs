namespace Digdir.Domain.Dialogporten.WebApi.Common.FeatureMetric;

/// <summary>
/// Configuration options for feature metric tracking.
/// </summary>
public sealed class FeatureMetricOptions
{
    /// <summary>
    /// Gets or sets the list of path prefixes to exclude from feature metric tracking.
    /// </summary>
    public List<string> ExcludedPathPrefixes { get; set; } = new()
    {
        "/health",
        "/metrics",
        "/swagger",
        "/openapi"
    };
}
