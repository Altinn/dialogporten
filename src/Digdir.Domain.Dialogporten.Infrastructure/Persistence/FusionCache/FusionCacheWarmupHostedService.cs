using Digdir.Domain.Dialogporten.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.FusionCache;

internal sealed class FusionCacheWarmupHostedService : IHostedService
{
    private readonly IFusionCache _cache;
    private readonly ILogger<FusionCacheWarmupHostedService> _logger;
    private readonly IConfiguration _configuration;

    public FusionCacheWarmupHostedService(
        IFusionCacheProvider cacheProvider,
        ILogger<FusionCacheWarmupHostedService> logger,
        IConfiguration configuration)
    {
        _cache = cacheProvider.GetCache(nameof(Altinn.ResourceRegistry)) ?? throw new ArgumentNullException(nameof(cacheProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_configuration.GetLocalDevelopmentSettings().DisableCache)
            {
                return;
            }

            _logger.LogInformation("Warming up FusionCache Redis backplane...");

            await _cache.TryGetAsync<string>(
                "__fusioncache-warmup-ping",
                options: new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromSeconds(30)
                }, token: cancellationToken);

            _logger.LogInformation("FusionCache backplane is ready.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FusionCache warmup failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
