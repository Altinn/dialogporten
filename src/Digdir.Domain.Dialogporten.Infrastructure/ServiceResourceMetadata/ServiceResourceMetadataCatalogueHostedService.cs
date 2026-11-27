using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.ServiceResourceMetadata;

internal sealed class ServiceResourceMetadataCatalogueHostedService : BackgroundService
{
    private static readonly TimeSpan InitInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(15);

    private readonly PeriodicTimer _timer = new(InitInterval);
    private readonly IServiceResourceMetadataCatalogue _serviceResourceMetadataCatalogue;
    private readonly ILogger<ServiceResourceMetadataCatalogueHostedService> _logger;

    public ServiceResourceMetadataCatalogueHostedService(
        IServiceResourceMetadataCatalogue serviceResourceMetadataCatalogue,
        ILogger<ServiceResourceMetadataCatalogueHostedService> logger
    )
    {
        ArgumentNullException.ThrowIfNull(serviceResourceMetadataCatalogue);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceResourceMetadataCatalogue = serviceResourceMetadataCatalogue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            await TryRefreshAsync(stoppingToken);
            _timer.Period = RefreshInterval;
        }
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _serviceResourceMetadataCatalogue.UpdateCatalogueCache(cancellationToken);
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(e, "An error occurred while building ServiceResourceMetadataCatalogue.");
            return false;
        }

        return true;
    }
}
