using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.HealthChecks;

internal sealed class RedisHealthCheck : IHealthCheck
{
    private readonly InfrastructureSettings _settings;
    private readonly ILogger<RedisHealthCheck> _logger;
    private const int DegradationThresholdInSeconds = 5;

    public RedisHealthCheck(IOptions<InfrastructureSettings> options, ILogger<RedisHealthCheck> logger)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var startTime = Stopwatch.GetTimestamp();
        try
        {
            const int timeout = 15_000;

            var options = ConfigurationOptions.Parse(_settings.Redis.ConnectionString);

            options.AsyncTimeout = timeout;
            options.ConnectTimeout = timeout;
            options.SyncTimeout = timeout;

            await using var redis = await ConnectionMultiplexer.ConnectAsync(options);
            var db = redis.GetDatabase();
            await db.PingAsync();

            var responseTime = Stopwatch.GetElapsedTime(startTime);

            if (responseTime > TimeSpan.FromSeconds(DegradationThresholdInSeconds))
            {
                _logger.LogWarning("Redis connection is slow ({Elapsed:N1}s).", responseTime.TotalSeconds);
                return HealthCheckResult.Degraded($"Redis connection is slow ({responseTime.TotalSeconds:N1}s).");
            }

            return HealthCheckResult.Healthy("Redis connection is healthy.");
        }
        catch (RedisTimeoutException ex)
        {
            var responseTime = Stopwatch.GetElapsedTime(startTime);
            _logger.LogWarning("Redis connection timed out ({Elapsed:N1}s).", responseTime.TotalSeconds);
            return HealthCheckResult.Degraded($"Redis connection timed out after {responseTime.TotalSeconds:N1}s.", exception: ex);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Unable to connect to Redis.");
            return HealthCheckResult.Degraded("Unable to connect to Redis.", exception: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while checking Redis' health.");
            return HealthCheckResult.Degraded("An unexpected error occurred while checking Redis' health.", exception: ex);
        }
    }
}
