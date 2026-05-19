using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Digdir.Domain.Dialogporten.Infrastructure.HealthChecks;

internal sealed class WarmupHealthCheck : IHealthCheck
{
    private readonly WarmupState _warmupState;

    public WarmupHealthCheck(WarmupState warmupState)
    {
        ArgumentNullException.ThrowIfNull(warmupState);

        _warmupState = warmupState;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = _warmupState.Status switch
        {
            WarmupStatus.Healthy => HealthCheckResult.Healthy("Readiness warmup completed."),
            WarmupStatus.Failed => HealthCheckResult.Unhealthy(
                $"Readiness warmup failed in phase '{_warmupState.FailedPhase ?? "unknown"}'.",
                _warmupState.Exception),
            WarmupStatus.Pending => HealthCheckResult.Unhealthy(
                $"Readiness warmup is pending in phase '{_warmupState.CurrentPhase ?? "not-started"}'."),
            _ => throw new InvalidOperationException($"Unknown warmup status '{_warmupState.Status}'.")
        };

        return Task.FromResult(result);
    }
}
