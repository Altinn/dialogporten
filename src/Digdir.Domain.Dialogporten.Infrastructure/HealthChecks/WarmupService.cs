using System.Security.Claims;
using System.Threading;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using AuthConstants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.Infrastructure.HealthChecks;

internal sealed partial class WarmupService : IHostedService
{
    private readonly ILogger<WarmupService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WarmupState _warmupState;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly WarmupSettings _settings;
    private CancellationTokenSource? _internalCts;
    private Task? _warmupTask;

    public WarmupService(
        ILogger<WarmupService> logger,
        IServiceScopeFactory scopeFactory,
        WarmupState warmupState,
        IHostApplicationLifetime hostApplicationLifetime,
        IOptions<InfrastructureSettings> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(warmupState);
        ArgumentNullException.ThrowIfNull(hostApplicationLifetime);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _scopeFactory = scopeFactory;
        _warmupState = warmupState;
        _hostApplicationLifetime = hostApplicationLifetime;
        _settings = options.Value.Warmup;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Readiness warmup is disabled.");
            _warmupState.MarkWarmupComplete();
            return Task.CompletedTask;
        }

        _logger.LogInformation("Queuing readiness warmup.");
        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _hostApplicationLifetime.ApplicationStopping);
        var warmupToken = _internalCts.Token;

        _warmupTask = Task.Run(() => PerformWarmupAsync(warmupToken), CancellationToken.None);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_warmupTask is null)
        {
            return;
        }

        var internalCts = Interlocked.Exchange(ref _internalCts, null);

        if (internalCts is not null)
        {
            await internalCts.CancelAsync();
        }

        try
        {
            await _warmupTask.WaitAsync(cancellationToken);
        }
        finally
        {
            internalCts?.Dispose();
        }
    }

    private async Task PerformWarmupAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;

            await RunPhaseAsync("db-pool", () => WarmupDbPoolAsync(services, timeoutCts.Token));
            await RunPhaseAsync("ef-model", () => WarmupEfModelAsync(services, timeoutCts.Token));
            await RunPhaseAsync("service-resource-metadata", () => WarmupServiceResourceMetadataAsync(services, timeoutCts.Token));

            if (_settings.RunEndUserSearch)
            {
                await RunPhaseAsync("end-user-search", () => WarmupEndUserSearchAsync(services, timeoutCts.Token));
            }

            _logger.LogInformation("Readiness warmup completed successfully.");
            _warmupState.MarkWarmupComplete();
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Readiness warmup timed out after {TimeoutSeconds}s.", _settings.TimeoutSeconds);
            _warmupState.MarkWarmupFailed("timeout", ex);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Readiness warmup was cancelled.");
            _warmupState.MarkWarmupFailed("cancelled", ex);
        }
        catch (ObjectDisposedException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Readiness warmup was cancelled because application services are stopping.");
            _warmupState.MarkWarmupFailed("cancelled", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness warmup failed.");
            _warmupState.MarkWarmupFailed(_warmupState.CurrentPhase ?? "unknown", ex);
        }
        finally
        {
            Interlocked.Exchange(ref _internalCts, null)?.Dispose();
        }
    }

    private async Task RunPhaseAsync(string phase, Func<Task> action)
    {
        _internalCts?.Token.ThrowIfCancellationRequested();
        _warmupState.MarkPhaseStarted(phase);
        WarmupPhaseStarting(_logger, phase);
        await action();
        WarmupPhaseCompleted(_logger, phase);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Starting readiness warmup phase {WarmupPhase}.")]
    private static partial void WarmupPhaseStarting(ILogger logger, string warmupPhase);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Completed readiness warmup phase {WarmupPhase}.")]
    private static partial void WarmupPhaseCompleted(ILogger logger, string warmupPhase);

    private async Task WarmupDbPoolAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var dataSource = services.GetRequiredService<NpgsqlDataSource>();
        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = _settings.DbConnectionParallelism
        };

        await Parallel.ForEachAsync(
            Enumerable.Range(0, _settings.DbConnectionsToOpen),
            options,
            async (_, token) =>
            {
                await using var connection = await dataSource.OpenConnectionAsync(token);
                await using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync(token);
            });
    }

    private static async Task WarmupEfModelAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<DialogDbContext>();
        await dbContext.DialogStatuses
            .AsNoTracking()
            .Where(x => x.Id == DialogStatus.Values.Completed)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task WarmupServiceResourceMetadataAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        try
        {
            var sender = services.GetRequiredService<ISender>();
            await sender.Send(new GetServiceResourceMetadataQuery(), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            ServiceResourceMetadataWarmupFailed(_logger, ex);
        }
    }

    private async Task WarmupEndUserSearchAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.EndUserPid))
        {
            EndUserSearchSkippedMissingPid(_logger);
            return;
        }

        try
        {
            using var _ = AmbientUserPrincipal.Use(new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimsPrincipalExtensions.ScopeClaim, "digdir:dialogporten"),
                new Claim(ClaimsPrincipalExtensions.PidClaim, _settings.EndUserPid),
                new Claim(ClaimsPrincipalExtensions.IdportenAuthLevelClaim, AuthConstants.IdportenLoaSubstantial)
            ], "WarmupAuth")));

            var sender = services.GetRequiredService<ISender>();
            var result = await sender.Send(new SearchDialogQuery
            {
                Party = [$"urn:altinn:person:identifier-no:{_settings.EndUserPid}"],
                Limit = 5
            }, cancellationToken);

            if (result.IsT0 && result.AsT0.Items.Count == 0)
            {
                EndUserSearchReturnedNoRows(_logger, _settings.EndUserPid);
            }
            else if (!result.IsT0)
            {
                EndUserSearchReturnedNonSuccess(_logger, result.Value.GetType().Name, _settings.EndUserPid);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            EndUserSearchFailed(_logger, _settings.EndUserPid, ex);
        }
    }

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Skipping end-user search warmup because Infrastructure:Warmup:EndUserPid is not configured.")]
    private static partial void EndUserSearchSkippedMissingPid(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "End-user search warmup returned no rows for PID {EndUserPid}.")]
    private static partial void EndUserSearchReturnedNoRows(ILogger logger, string endUserPid);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "End-user search warmup returned {ResultType} for PID {EndUserPid}.")]
    private static partial void EndUserSearchReturnedNonSuccess(ILogger logger, string resultType, string endUserPid);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "End-user search warmup failed for PID {EndUserPid}; readiness will not be failed by this optional phase.")]
    private static partial void EndUserSearchFailed(ILogger logger, string endUserPid, Exception exception);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Service resource metadata warmup failed; readiness will not be failed by this optional phase.")]
    private static partial void ServiceResourceMetadataWarmupFailed(ILogger logger, Exception exception);
}

internal enum WarmupStatus
{
    Pending,
    Healthy,
    Failed
}

internal sealed class WarmupState
{
    private readonly Lock _lock = new();

    public WarmupStatus Status { get; private set; } = WarmupStatus.Pending;
    public string? CurrentPhase { get; private set; }
    public string? FailedPhase { get; private set; }
    public Exception? Exception { get; private set; }
    public bool IsWarmupComplete => Status == WarmupStatus.Healthy;

    public void MarkPhaseStarted(string phase)
    {
        lock (_lock)
        {
            CurrentPhase = phase;
        }
    }

    public void MarkWarmupComplete()
    {
        lock (_lock)
        {
            Status = WarmupStatus.Healthy;
            CurrentPhase = null;
            FailedPhase = null;
            Exception = null;
        }
    }

    public void MarkWarmupFailed(string phase, Exception ex)
    {
        lock (_lock)
        {
            Status = WarmupStatus.Failed;
            CurrentPhase = null;
            FailedPhase = phase;
            Exception = ex;
        }
    }
}
