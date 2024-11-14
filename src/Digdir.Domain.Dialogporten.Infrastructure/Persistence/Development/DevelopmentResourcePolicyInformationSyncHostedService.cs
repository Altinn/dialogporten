using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncPolicy;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Development;

internal sealed class DevelopmentResourcePolicyInformationSyncHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public DevelopmentResourcePolicyInformationSyncHostedService(IServiceProvider serviceProvider, IHostEnvironment environment, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.ShouldRunDevelopmentHostedService() || _configuration.GetLocalDevelopmentSettings().DisablePolicyInformationSyncOnStartup)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new SyncPolicyCommand(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
