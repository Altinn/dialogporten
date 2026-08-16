using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Common.Caching;

/// <summary>
/// Runs FusionCache factories with their own dependency lifetime, work ceiling, and resource bound.
///
/// FusionCache detaches factories from the triggering request: a factory that exceeds FactorySoftTimeout keeps
/// running as a background completion, and eager refresh starts factories on a request context that is about to
/// unwind. FactoryHardTimeout only releases waiters; it never cancels the factory, and the per-key memory lock
/// is released only when the factory task completes. A factory must therefore:
/// own its dependencies for its full lifetime (a dedicated DI scope instead of captured request-scoped
/// services), enforce its own work ceiling (cooperative cancellation at CancellationAfter, hard abandonment at
/// AbandonAfter so a stuck factory cannot hold the per-key lock), and be bounded in how many executions can be
/// live at once (a pre-execution permit per cache) so abandonment cannot convert a per-key lock wedge into
/// connection-pool exhaustion.
/// </summary>
internal sealed partial class FusionCacheFactoryRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FusionCacheFactoryRunner> _logger;
    private readonly ConcurrentDictionary<string, BulkheadState> _bulkheads = new(StringComparer.Ordinal);

    public FusionCacheFactoryRunner(IServiceScopeFactory scopeFactory, ILogger<FusionCacheFactoryRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Touch every policy's instruments so each cache has a zero-valued series from startup instead of
        // appearing only on first activity.
        foreach (var policy in FusionCacheFactoryPolicy.All)
        {
            FusionCacheFactoryTelemetry.ActiveExecutions.Add(0, CacheTag(policy));
            FusionCacheFactoryTelemetry.ActiveOrphans.Add(0, CacheTag(policy));
        }
    }

    /// <summary>
    /// Runs a factory that needs scoped services (DbContext, IOptionsSnapshot, ...) in a dedicated DI scope
    /// that lives exactly as long as the factory execution, detached or not.
    /// </summary>
    public Task<TValue> RunInScope<TValue>(
        FusionCacheFactoryPolicy policy,
        Func<IServiceProvider, CancellationToken, Task<TValue>> factory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(factory);
        return RunShared(policy, async ct =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            return await factory(scope.ServiceProvider, ct);
        }, cancellationToken);
    }

    /// <summary>
    /// Runs a factory on singleton dependencies (no DI scope) with the same permits and deadlines.
    /// </summary>
    public Task<TValue> Run<TValue>(
        FusionCacheFactoryPolicy policy,
        Func<CancellationToken, Task<TValue>> factory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(factory);
        return RunShared(policy, factory, cancellationToken);
    }

    private async Task<TValue> RunShared<TValue>(
        FusionCacheFactoryPolicy policy,
        Func<CancellationToken, Task<TValue>> factory,
        CancellationToken cancellationToken)
    {
        // A pre-cancelled call reports cancellation, not capacity rejection.
        cancellationToken.ThrowIfCancellationRequested();

        var lease = AcquireLease(policy);
        var start = Stopwatch.GetTimestamp();
        CancellationTokenSource? cancellationSource = null;
        Task<TValue> factoryTask;
        try
        {
            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationSource.CancelAfter(policy.CancellationAfter);
            // Ownership of the lease and the cancellation source transfers to the factory task, whose
            // finally releases them when the execution ACTUALLY completes - even long after abandonment.
            factoryTask = RunCore(policy, factory, lease, cancellationSource, cancellationToken);
        }
        catch
        {
            cancellationSource?.Dispose();
            lease.Dispose();
            throw;
        }

        try
        {
            // Both timers were scheduled before any user code ran; pass the REMAINING abandonment budget so
            // cooperative cancellation and abandonment share one start point.
            var remaining = policy.AbandonAfter - Stopwatch.GetElapsedTime(start);
            return await factoryTask.WaitAsync(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, cancellationToken);
        }
        catch (TimeoutException)
        {
            if (factoryTask.IsCompleted)
            {
                // The factory completed concurrently with the timer; propagate its actual outcome.
                return await factoryTask;
            }

            RegisterOrphan(policy, factoryTask, start, deadlineAbandoned: true);
            var abandonment = new OperationCanceledException(
                $"The '{policy.CacheName}' cache factory did not complete within its {policy.AbandonAfter} abandonment ceiling.");
            LogFactoryAbandoned(policy.CacheName, policy.CancellationAfter, policy.AbandonAfter, abandonment);
            // FusionCache observes a cancelled factory and releases the per-key lock; waiters were already
            // released (or served stale) at FactoryHardTimeout.
            throw abandonment;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (!factoryTask.IsCompleted)
            {
                // Caller cancellation is not a failure, but the still-running execution must be tracked like
                // any other orphan: it keeps its permit and scope until it actually completes.
                RegisterOrphan(policy, factoryTask, start, deadlineAbandoned: false);
            }

            throw;
        }
    }

    private static async Task<TValue> RunCore<TValue>(
        FusionCacheFactoryPolicy policy,
        Func<CancellationToken, Task<TValue>> factory,
        ExecutionLease lease,
        CancellationTokenSource cancellationSource,
        CancellationToken outerToken)
    {
        try
        {
            // Return to the wrapper before any user code runs, so a factory that blocks synchronously before
            // its first await can still be abandoned by the wall-clock ceiling.
            await Task.Yield();
            // Skip the work (and the scope) entirely if cancellation or abandonment won while this
            // continuation was queued.
            cancellationSource.Token.ThrowIfCancellationRequested();
            return await factory(cancellationSource.Token);
        }
        catch (OperationCanceledException) when (outerToken.IsCancellationRequested)
        {
            // Caller/shutdown cancellation is not a factory failure.
            throw;
        }
        catch (OperationCanceledException ex) when (cancellationSource.IsCancellationRequested)
        {
            lease.LogFactoryDeadlineCancelled(policy.CancellationAfter, ex);
            throw;
        }
        catch (FusionCacheFactoryRejectedException)
        {
            // A nested factory's capacity rejection is already logged as a circuit transition by its own
            // runner invocation; logging it per-occurrence here would recreate the log storm.
            throw;
        }
        catch (Exception ex)
        {
            // Includes OperationCanceledException that neither the caller nor this runner requested: such
            // cancellation is an unexpected factory failure and must stay attributable.
            lease.LogFactoryFailed(ex);
            throw;
        }
        finally
        {
            cancellationSource.Dispose();
            lease.Dispose();
        }
    }

    private ExecutionLease AcquireLease(FusionCacheFactoryPolicy policy)
    {
        var bulkhead = _bulkheads.GetOrAdd(
            policy.CacheName,
            static (_, maxConcurrentExecutions) => new BulkheadState(maxConcurrentExecutions),
            policy.MaxConcurrentExecutions);

        if (!bulkhead.Permits.Wait(0))
        {
            // Log the transition into the exhausted interval exactly once (on the first rejection), never
            // per rejection.
            if (Interlocked.CompareExchange(ref bulkhead.ExhaustionLogged, 1, 0) == 0)
            {
                LogCapacityExhausted(policy.CacheName, policy.MaxConcurrentExecutions);
            }

            throw new FusionCacheFactoryRejectedException(
                $"The '{policy.CacheName}' cache factory was rejected: all {policy.MaxConcurrentExecutions} " +
                "execution permits are in use by running or abandoned factories.");
        }

        FusionCacheFactoryTelemetry.ActiveExecutions.Add(1, CacheTag(policy));
        return new ExecutionLease(this, policy, bulkhead);
    }

    private void RegisterOrphan<TValue>(FusionCacheFactoryPolicy policy, Task<TValue> factoryTask, long start, bool deadlineAbandoned)
    {
        FusionCacheFactoryTelemetry.ActiveOrphans.Add(1, CacheTag(policy));
        _ = ObserveOrphanAsync(policy, factoryTask, start, deadlineAbandoned);
    }

    private async Task ObserveOrphanAsync<TValue>(FusionCacheFactoryPolicy policy, Task<TValue> factoryTask, long start, bool deadlineAbandoned)
    {
        Exception? failure = null;
        try
        {
            // SuppressThrowing is only valid on the non-generic Task.
            await ((Task)factoryTask).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            if (factoryTask.Exception is not null)
            {
                failure = factoryTask.Exception.GetBaseException();
            }
        }
        finally
        {
            FusionCacheFactoryTelemetry.ActiveOrphans.Add(-1, CacheTag(policy));
        }

        var elapsed = Stopwatch.GetElapsedTime(start);
        if (deadlineAbandoned)
        {
            LogOrphanCompleted(policy.CacheName, elapsed, failure);
        }
        else
        {
            LogCancelledOrphanCompleted(policy.CacheName, elapsed);
        }
    }

    private static KeyValuePair<string, object?> CacheTag(FusionCacheFactoryPolicy policy) =>
        new(FusionCacheFactoryTelemetry.CacheNameTag, policy.CacheName);

    [LoggerMessage(EventId = 1, EventName = "FusionCacheFactoryFailed", Level = LogLevel.Error,
        Message = "The {CacheName} cache factory failed.")]
    private partial void LogFactoryFailed(string cacheName, Exception exception);

    [LoggerMessage(EventId = 2, EventName = "FusionCacheFactoryDeadlineCancelled", Level = LogLevel.Error,
        Message = "The {CacheName} cache factory was cancelled at its {CancellationAfter} cancellation deadline.")]
    private partial void LogFactoryDeadlineCancelled(string cacheName, TimeSpan cancellationAfter, Exception exception);

    [LoggerMessage(EventId = 3, EventName = "FusionCacheFactoryAbandoned", Level = LogLevel.Error,
        Message = "The {CacheName} cache factory ignored cancellation at {CancellationAfter} and was abandoned at its {AbandonAfter} ceiling; it keeps its execution permit until it completes.")]
    private partial void LogFactoryAbandoned(string cacheName, TimeSpan cancellationAfter, TimeSpan abandonAfter, Exception exception);

    [LoggerMessage(EventId = 4, EventName = "FusionCacheFactoryOrphanCompleted", Level = LogLevel.Warning,
        Message = "An abandoned {CacheName} cache factory completed after {Elapsed}.")]
    private partial void LogOrphanCompleted(string cacheName, TimeSpan elapsed, Exception? exception);

    [LoggerMessage(EventId = 5, EventName = "FusionCacheFactoryCapacityExhausted", Level = LogLevel.Error,
        Message = "All {MaxConcurrentExecutions} execution permits for the {CacheName} cache factory are in use; new factory invocations are rejected until one completes.")]
    private partial void LogCapacityExhausted(string cacheName, int maxConcurrentExecutions);

    [LoggerMessage(EventId = 6, EventName = "FusionCacheFactoryCapacityRestored", Level = LogLevel.Information,
        Message = "Execution permits for the {CacheName} cache factory are available again.")]
    private partial void LogCapacityRestored(string cacheName);

    [LoggerMessage(EventId = 7, EventName = "FusionCacheFactoryCancelledOrphanCompleted", Level = LogLevel.Debug,
        Message = "A caller-cancelled {CacheName} cache factory completed after {Elapsed}.")]
    private partial void LogCancelledOrphanCompleted(string cacheName, TimeSpan elapsed);

    private sealed class BulkheadState
    {
        public BulkheadState(int maxConcurrentExecutions)
        {
            Permits = new SemaphoreSlim(maxConcurrentExecutions, maxConcurrentExecutions);
        }

        public SemaphoreSlim Permits { get; }

        // 1 while an exhausted interval has been logged (EventId 5); reset (with EventId 6) when a permit frees.
        public int ExhaustionLogged;
    }

    /// <summary>
    /// Owns one execution permit and the matching counter decrement. Idempotent: whichever of the wrapper's
    /// failure path or the factory task's finally runs last cannot double-release.
    /// </summary>
    private sealed class ExecutionLease : IDisposable
    {
        private readonly FusionCacheFactoryRunner _runner;
        private readonly FusionCacheFactoryPolicy _policy;
        private readonly BulkheadState _bulkhead;
        private int _disposed;

        public ExecutionLease(FusionCacheFactoryRunner runner, FusionCacheFactoryPolicy policy, BulkheadState bulkhead)
        {
            _runner = runner;
            _policy = policy;
            _bulkhead = bulkhead;
        }

        public void LogFactoryFailed(Exception exception) =>
            _runner.LogFactoryFailed(_policy.CacheName, exception);

        public void LogFactoryDeadlineCancelled(TimeSpan cancellationAfter, Exception exception) =>
            _runner.LogFactoryDeadlineCancelled(_policy.CacheName, cancellationAfter, exception);

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            _bulkhead.Permits.Release();
            FusionCacheFactoryTelemetry.ActiveExecutions.Add(-1, CacheTag(_policy));

            // Close the exhausted interval (EventId 6) only if EventId 5 opened it.
            if (Interlocked.CompareExchange(ref _bulkhead.ExhaustionLogged, 0, 1) == 1)
            {
                _runner.LogCapacityRestored(_policy.CacheName);
            }
        }
    }
}
