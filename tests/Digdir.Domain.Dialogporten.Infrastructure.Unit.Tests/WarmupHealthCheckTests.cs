using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.Infrastructure.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public sealed class WarmupHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenWarmupIsPending()
    {
        var state = new WarmupState();
        var healthCheck = new WarmupHealthCheck(state);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("pending", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenWarmupCompleted()
    {
        var state = new WarmupState();
        state.MarkWarmupComplete();
        var healthCheck = new WarmupHealthCheck(state);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenWarmupFailed()
    {
        var state = new WarmupState();
        var exception = new InvalidOperationException("boom");
        state.MarkWarmupFailed("ef-model", exception);
        var healthCheck = new WarmupHealthCheck(state);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Same(exception, result.Exception);
        Assert.Contains("ef-model", result.Description);
    }

    [Fact]
    public async Task StartAsync_MarksWarmupComplete_WhenWarmupIsDisabled()
    {
        var state = new WarmupState();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var service = new WarmupService(
            NullLogger<WarmupService>.Instance,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            state,
            Options.Create(CreateInfrastructureSettings(new WarmupSettings { Enabled = false })));

        await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.True(state.IsWarmupComplete);
    }

    [Fact]
    public void MarkPhaseStarted_ExposesPendingPhase()
    {
        var state = new WarmupState();

        state.MarkPhaseStarted("db-pool");

        Assert.Equal(WarmupStatus.Pending, state.Status);
        Assert.Equal("db-pool", state.CurrentPhase);
    }

    private static InfrastructureSettings CreateInfrastructureSettings(WarmupSettings warmupSettings) =>
        new()
        {
            DialogDbConnectionString = "Host=localhost;Database=dialogporten",
            Redis = new RedisSettings { ConnectionString = "localhost" },
            Altinn = new AltinnPlatformSettings
            {
                BaseUri = new Uri("https://platform.altinn.no/"),
                EventsBaseUri = new Uri("https://platform.altinn.no/"),
                SubscriptionKey = "test"
            },
            AltinnCdn = new AltinnCdnPlatformSettings
            {
                BaseUri = new Uri("https://altinncdn.no/")
            },
            Maskinporten = new()
            {
                Environment = "test",
                ClientId = "test",
                Scope = "test",
                EncodedJwk = "test"
            },
            MassTransit = new MassTransitSettings { Host = "localhost" },
            DialogSearch = new DialogSearchSettings(),
            Warmup = warmupSettings
        };
}
