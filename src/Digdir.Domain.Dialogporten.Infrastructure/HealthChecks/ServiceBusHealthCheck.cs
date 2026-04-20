using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.HealthChecks;

// This is an application-level wrapper around MassTransit's built-in health check.
// MassTransit owns the actual bus state probing; this wrapper controls how that state
// is exposed from Dialogporten's public health endpoints.
internal sealed class ServiceBusHealthCheck(HealthCheckService healthCheckService, ILogger<ServiceBusHealthCheck> logger) : IHealthCheck
{
    // These identify the inner MassTransit check. It is intentionally not tagged as a
    // public dependency, otherwise /health would show both the raw MassTransit check
    // and this app-level "servicebus" check.
    internal const string InnerHealthCheckName = "masstransit-servicebus";
    internal const string InnerHealthCheckTag = "masstransit-servicebus-internal";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // The predicate limits execution to MassTransit's own check, so this wrapper does not run the
        // full application health-check set recursively.
        var report = await healthCheckService.CheckHealthAsync(IsMassTransitServiceBusHealthCheck, cancellationToken);

        if (report.Entries.Count == 0)
        {
            // Missing inner check means local configuration is wrong. That differs from
            // Azure Service Bus being unavailable, which the outbox can tolerate.
            const string description = "MassTransit Service Bus health check is not registered.";
            logger.LogError(description);
            return HealthCheckResult.Unhealthy(description);
        }

        if (report.Status == HealthStatus.Healthy)
        {
            return HealthCheckResult.Healthy("Azure Service Bus is healthy.");
        }

        var exception = report.Entries.Values
            .Select(static entry => entry.Exception)
            .FirstOrDefault(static exception => exception is not null);

        // Service Bus outages should not make this app unhealthy: PostgreSQL keeps
        // outbound messages in the outbox until broker connectivity recovers.
        logger.LogWarning(exception, "Azure Service Bus health check reported {Status}.", report.Status);
        return HealthCheckResult.Degraded($"Azure Service Bus is {report.Status.ToString().ToLowerInvariant()}.", exception: exception);
    }

    private static bool IsMassTransitServiceBusHealthCheck(HealthCheckRegistration registration) =>
        registration.Name.Equals(InnerHealthCheckName, StringComparison.Ordinal)
        || registration.Tags.Contains(InnerHealthCheckTag);
}
