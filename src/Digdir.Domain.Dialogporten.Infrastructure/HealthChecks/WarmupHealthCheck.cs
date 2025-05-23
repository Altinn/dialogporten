using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Digdir.Domain.Dialogporten.Infrastructure.HealthChecks;

public class WarmupHealthCheck : IHealthCheck
{
    private readonly WarmupState _warmupState;
    private readonly ILogger<WarmupHealthCheck> _logger;

    public WarmupHealthCheck(WarmupState warmupState, ILogger<WarmupHealthCheck> logger)
    {
        _warmupState = warmupState;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_warmupState.IsWarmupComplete)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Application warmup completed."));
        }

        var warmupTask = _warmupState.WaitForWarmupAsync(CancellationToken.None); // Use None here, health check timeout applies
        if (warmupTask.IsFaulted)
        {
            _logger.LogWarning("WarmupHealthCheck: Warmup failed: Exception: {Exception}.", warmupTask.Exception?.InnerException);
            return Task.FromResult(HealthCheckResult.Unhealthy("Warmup failed.", warmupTask.Exception?.InnerException));
        }

        _logger.LogInformation("WarmupHealthCheck: Warmup is still in progress.");
        return Task.FromResult(HealthCheckResult.Unhealthy("Warmup is still in progress."));
    }
}
