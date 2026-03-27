using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Digdir.Library.Utils.AspNet.HealthChecks;

internal sealed class EndpointsHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EndpointsHealthCheck> _logger;
    private readonly List<string> _endpoints;
    private const int DegradationThresholdInSeconds = 5;
    private const int TimeoutInSeconds = 40;

    public EndpointsHealthCheck(
        IHttpClientFactory httpClientFactory,
        ILogger<EndpointsHealthCheck> logger,
        IOptions<AspNetUtilitiesSettings> options)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _endpoints = options.Value.HealthCheckSettings.HttpGetEndpointsToCheck;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var unhealthyEndpoints = new ConcurrentBag<string>();

        var tasks = _endpoints.Select(async url =>
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(TimeoutInSeconds));

                var startTime = Stopwatch.GetTimestamp();
                var response = await client.GetAsync(url, cts.Token);
                var responseTime = Stopwatch.GetElapsedTime(startTime);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Health check failed for endpoint: {Url}. Status Code: {StatusCode}", url, response.StatusCode);
                    unhealthyEndpoints.Add($"{url} (Status Code: {response.StatusCode})");
                }
                else if (responseTime > TimeSpan.FromSeconds(DegradationThresholdInSeconds))
                {
                    _logger.LogWarning("Health check response was slow for endpoint: {Url}. Elapsed time: {Elapsed:N1}s", url, responseTime.TotalSeconds);
                    unhealthyEndpoints.Add($"{url} (Degraded - Response time: {responseTime.TotalSeconds:N1}s)");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Health check timed out for endpoint: {Url}", url);
                unhealthyEndpoints.Add($"{url} (Timeout after {TimeoutInSeconds}s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while checking endpoint: {Url}", url);
                unhealthyEndpoints.Add($"{url} (Exception: {ex.GetType().Name})");
            }
        });

        await Task.WhenAll(tasks);

        if (unhealthyEndpoints.IsEmpty)
        {
            return HealthCheckResult.Healthy("All endpoints are healthy.");
        }

        var description = $"The following endpoints are unhealthy: {string.Join(", ", unhealthyEndpoints)}";
        return HealthCheckResult.Unhealthy(description);
    }
}
