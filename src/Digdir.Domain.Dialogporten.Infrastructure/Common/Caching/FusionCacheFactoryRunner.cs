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
    // One entry per policy: policies are static per cache, never per key, so this dictionary is bounded by
    // FusionCacheFactoryPolicy.All and is deliberately never pruned. A per-key policy would leak here.
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
            LogFactoryAbandoned(_logger, policy.CacheName, policy.CancellationAfter, policy.AbandonAfter, abandonment);
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
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationSource.Token)
        {
            // Matched by token identity, not source state: this event measures whether cooperative
            // cancellation works, so a cancellation the factory produced independently after the deadline
            // elapsed must not masquerade as a deadline cancellation. (The caller-cancellation clause above
            // deliberately stays state-based: once the caller is gone, all cancellation fallout is noise.)
            // Token-less cancellations that were in fact responses to our token fall through to the failure
            // log below, which errs toward attribution.
            lease.ReportFactoryDeadlineCancelled(policy.CancellationAfter, ex);
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
            // Includes OperationCanceledException carrying neither the caller's nor this runner's token: such
            // cancellation is an unexpected factory failure and must stay attributable.
            lease.ReportFactoryFailed(ex);
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

        var rejected = false;
        var logExhaustion = false;
        lock (bulkhead.Gate)
        {
            if (bulkhead.AvailablePermits == 0)
            {
                rejected = true;
                // Mark the transition into the exhausted interval exactly once (on the first rejection),
                // never per rejection.
                if (!bulkhead.ExhaustionLogged)
                {
                    bulkhead.ExhaustionLogged = true;
                    logExhaustion = true;
                }
            }
            else
            {
                bulkhead.AvailablePermits--;
            }
        }

        if (rejected)
        {
            // Logged outside the gate: logging providers are third-party code that must not run under the
            // permit lock (same rule as the metric callbacks below). The transition decision is made under
            // the gate, so each exhausted interval still logs exactly once; only the emission order against
            // a concurrent restore can wobble.
            if (logExhaustion)
            {
                LogCapacityExhausted(_logger, policy.CacheName, policy.MaxConcurrentExecutions);
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
            LogOrphanCompleted(_logger, policy.CacheName, elapsed, failure);
        }
        else
        {
            LogCancelledOrphanCompleted(_logger, policy.CacheName, elapsed);
        }
    }

    private static KeyValuePair<string, object?> CacheTag(FusionCacheFactoryPolicy policy) =>
        new(FusionCacheFactoryTelemetry.CacheNameTag, policy.CacheName);

    [LoggerMessage(EventId = 1, EventName = "FusionCacheFactoryFailed", Level = LogLevel.Error,
        Message = "The {CacheName} cache factory failed.")]
    private static partial void LogFactoryFailed(ILogger logger, string cacheName, Exception exception);

    [LoggerMessage(EventId = 2, EventName = "FusionCacheFactoryDeadlineCancelled", Level = LogLevel.Error,
        Message = "The {CacheName} cache factory was cancelled at its {CancellationAfter} cancellation deadline.")]
    private static partial void LogFactoryDeadlineCancelled(ILogger logger, string cacheName, TimeSpan cancellationAfter, Exception exception);

    [LoggerMessage(EventId = 3, EventName = "FusionCacheFactoryAbandoned", Level = LogLevel.Error,
        Message = "The {CacheName} cache factory ignored cancellation at {CancellationAfter} and was abandoned at its {AbandonAfter} ceiling; it keeps its execution permit until it completes.")]
    private static partial void LogFactoryAbandoned(ILogger logger, string cacheName, TimeSpan cancellationAfter, TimeSpan abandonAfter, Exception exception);

    [LoggerMessage(EventId = 4, EventName = "FusionCacheFactoryOrphanCompleted", Level = LogLevel.Warning,
        Message = "An abandoned {CacheName} cache factory completed after {Elapsed}.")]
    private static partial void LogOrphanCompleted(ILogger logger, string cacheName, TimeSpan elapsed, Exception? exception);

    [LoggerMessage(EventId = 5, EventName = "FusionCacheFactoryCapacityExhausted", Level = LogLevel.Error,
        Message = "All {MaxConcurrentExecutions} execution permits for the {CacheName} cache factory are in use; new factory invocations are rejected until one completes.")]
    private static partial void LogCapacityExhausted(ILogger logger, string cacheName, int maxConcurrentExecutions);

    [LoggerMessage(EventId = 6, EventName = "FusionCacheFactoryCapacityRestored", Level = LogLevel.Information,
        Message = "Execution permits for the {CacheName} cache factory are available again.")]
    private static partial void LogCapacityRestored(ILogger logger, string cacheName);

    [LoggerMessage(EventId = 7, EventName = "FusionCacheFactoryCancelledOrphanCompleted", Level = LogLevel.Debug,
        Message = "A caller-cancelled {CacheName} cache factory completed after {Elapsed}.")]
    private static partial void LogCancelledOrphanCompleted(ILogger logger, string cacheName, TimeSpan elapsed);

    private sealed class BulkheadState
    {
        public BulkheadState(int maxConcurrentExecutions)
        {
            AvailablePermits = maxConcurrentExecutions;
        }

        // Permit accounting and the exhausted-interval flag (EventId 5 logged / awaiting EventId 6) must
        // transition together under the gate: releasing a permit before resetting the flag would let a
        // re-acquire-then-reject interleaving observe a stale interval and swallow its EventId 5 while the
        // original release still emits EventId 6.
        public Lock Gate { get; } = new();
        public int AvailablePermits;
        public bool ExhaustionLogged;
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

        public void ReportFactoryFailed(Exception exception) =>
            LogFactoryFailed(_runner._logger, _policy.CacheName, exception);

        public void ReportFactoryDeadlineCancelled(TimeSpan cancellationAfter, Exception exception) =>
            LogFactoryDeadlineCancelled(_runner._logger, _policy.CacheName, cancellationAfter, exception);

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            var logRestored = false;
            lock (_bulkhead.Gate)
            {
                _bulkhead.AvailablePermits++;

                // Close the exhausted interval (EventId 6) only if EventId 5 opened it, atomically with the
                // permit release so the permit cannot be re-acquired and re-exhausted before the interval
                // closes.
                if (_bulkhead.ExhaustionLogged)
                {
                    _bulkhead.ExhaustionLogged = false;
                    logRestored = true;
                }
            }

            // Logged outside the gate: a blocked logging provider must not stall permit release, least of
            // all while the bulkhead is describing its own exhaustion.
            if (logRestored)
            {
                LogCapacityRestored(_runner._logger, _policy.CacheName);
            }

            // Deliberately outside the gate: measurement callbacks (MeterListener) run inline on Add and must
            // not execute under the permit lock. The counter can transiently overshoot when a release and a
            // re-acquire interleave; it is a running sum read at export resolution, so the skew self-corrects.
            FusionCacheFactoryTelemetry.ActiveExecutions.Add(-1, CacheTag(_policy));
        }
    }
}
