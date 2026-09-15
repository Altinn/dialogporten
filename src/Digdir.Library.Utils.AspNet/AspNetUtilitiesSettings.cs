namespace Digdir.Library.Utils.AspNet;

/// <summary>
/// An outbound health probe registered from code, for upstreams that live in another settings
/// section than the probe list - the JWT bearer well-known metadata endpoints. Configuration-driven
/// probes bind to the probes package's own type instead.
/// </summary>
/// <param name="Name">The health check name. Must be unique across the application.</param>
/// <param name="Url">Absolute URL to probe with HTTP GET.</param>
/// <param name="Hard">
/// Whether a failure makes /health/deep fail (Unhealthy) rather than only degrade it.
/// </param>
public sealed record HealthProbe(string Name, string Url, bool Hard = false);

public sealed class TelemetrySettings
{
    private const string MassTransitSource = "MassTransit";
    private const string AzureSource = "Azure.*";

    public string? ServiceName { get; set; }
    public string? Endpoint { get; set; }
    public string? Protocol { get; set; }
    public string? AppInsightsConnectionString { get; set; }
    // Expected format: key1=value1,key2=value2
    public string? ResourceAttributes { get; set; }
    public HashSet<string> TraceSources { get; set; } =
    [
        AzureSource,
        MassTransitSource
    ];
}
