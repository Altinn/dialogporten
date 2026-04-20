using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Digdir.Library.Utils.AspNet.HealthChecks;

internal sealed class EndpointsHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EndpointsHealthCheck> _logger;
    private readonly List<HttpGetEndpointToCheck> _endpoints;
    private const int DegradationThresholdInSeconds = 5;
    private const int TimeoutInSeconds = 20;

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

        var results = await Task.WhenAll(_endpoints.Select(endpoint => CheckEndpointAsync(client, endpoint, cancellationToken)));
        var hardFailures = results.Where(result =>
            result is { Status: EndpointStatus.Failed, Endpoint.HardDependency: true }).ToList();
        var degradedResults = results.Where(result =>
            result.Status == EndpointStatus.Slow || result is { Status: EndpointStatus.Failed, Endpoint.HardDependency: false }).ToList();
        var data = CreateHealthCheckData(results, hardFailures.Count);

        if (hardFailures.Count == 0 && degradedResults.Count == 0)
        {
            return HealthCheckResult.Healthy("All endpoints are healthy.", data);
        }

        List<string> descriptionParts = [];

        if (hardFailures.Count != 0)
        {
            descriptionParts.Add($"Hard dependency failures: {string.Join(", ", hardFailures.Select(result => result.Description))}");
        }

        if (degradedResults.Count != 0)
        {
            descriptionParts.Add($"Degraded endpoints: {string.Join(", ", degradedResults.Select(result => result.Description))}");
        }

        var description = string.Join(". ", descriptionParts);
        return hardFailures.Count != 0
            ? HealthCheckResult.Unhealthy(description, data: data)
            : HealthCheckResult.Degraded(description, data: data);
    }

    private async Task<EndpointCheckResult> CheckEndpointAsync(HttpClient client, HttpGetEndpointToCheck endpoint, CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.GetTimestamp();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(TimeoutInSeconds));

            using var response = await client.GetAsync(endpoint.Url, cts.Token);
            var responseTime = Stopwatch.GetElapsedTime(startTime);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Health check failed for endpoint: {EndpointName} ({Url}). Status Code: {StatusCode}", endpoint.Name, endpoint.Url, response.StatusCode);
                return new(endpoint, EndpointStatus.Failed, responseTime, $"{endpoint.Name} ({endpoint.Url}) failed with status code {(int)response.StatusCode}");
            }

            if (responseTime > TimeSpan.FromSeconds(DegradationThresholdInSeconds))
            {
                _logger.LogWarning("Health check response was slow for endpoint: {EndpointName} ({Url}). Elapsed time: {Elapsed:N1}s", endpoint.Name, endpoint.Url, responseTime.TotalSeconds);
                return new(endpoint, EndpointStatus.Slow, responseTime, $"{endpoint.Name} ({endpoint.Url}) responded slowly in {responseTime.TotalSeconds:N1}s");
            }

            return new(endpoint, EndpointStatus.Healthy, responseTime, $"{endpoint.Name} ({endpoint.Url}) is healthy");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Propagate caller/request aborts; only the linked CTS timeout below should be reported as endpoint failure.
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Health check timed out for endpoint: {EndpointName} ({Url})", endpoint.Name, endpoint.Url);
            return new(endpoint, EndpointStatus.Failed, Stopwatch.GetElapsedTime(startTime), $"{endpoint.Name} ({endpoint.Url}) timed out after {TimeoutInSeconds}s");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while checking endpoint: {EndpointName} ({Url})", endpoint.Name, endpoint.Url);
            return new(endpoint, EndpointStatus.Failed, Stopwatch.GetElapsedTime(startTime), $"{endpoint.Name} ({endpoint.Url}) failed with {ex.GetType().Name}");
        }
    }

    private static Dictionary<string, object> CreateHealthCheckData(
        IReadOnlyCollection<EndpointCheckResult> results,
        int hardFailureCount)
    {
        var slowCount = results.Count(result => result.Status == EndpointStatus.Slow);
        var softFailureCount = results.Count(result => result is { Status: EndpointStatus.Failed, Endpoint.HardDependency: false });

        return new Dictionary<string, object>
        {
            ["checkedEndpoints"] = results.Select(result => new Dictionary<string, object>
            {
                ["name"] = result.Endpoint.Name,
                ["url"] = result.Endpoint.Url ?? string.Empty,
                ["hardDependency"] = result.Endpoint.HardDependency,
                ["status"] = result.Status.ToString(),
                ["durationMs"] = result.Duration.TotalMilliseconds
            }).ToList(),
            ["totalCount"] = results.Count,
            ["hardFailureCount"] = hardFailureCount,
            ["softFailureCount"] = softFailureCount,
            ["slowCount"] = slowCount
        };
    }

    private sealed record EndpointCheckResult(HttpGetEndpointToCheck Endpoint, EndpointStatus Status, TimeSpan Duration, string Description);

    private enum EndpointStatus
    {
        Healthy,
        Slow,
        Failed
    }
}
