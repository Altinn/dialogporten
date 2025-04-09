using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncSubjectMap;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Development;

internal sealed class DevelopmentSubjectResourceSyncHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public DevelopmentSubjectResourceSyncHostedService(IServiceProvider serviceProvider, IHostEnvironment environment, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment() || _configuration.GetLocalDevelopmentSettings().DisableSubjectResourceSyncOnStartup)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new SyncSubjectMapCommand(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
