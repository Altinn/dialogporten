using System.Diagnostics.Metrics;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Infrastructure.Common.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

// Verifies the three guarantees FusionCacheFactoryRunner exists to provide: a factory owns its dependencies
// via a dedicated DI scope (so detached executions never observe a disposed request scope), a factory has a
// wall-clock ceiling (cooperative cancellation, then hard abandonment that lets FusionCache release its
// per-key lock), and executions are bounded per cache (abandoned work cannot exhaust shared resources).
//
// Timing style: TaskCompletionSource gates + WaitUntil polling with generous deadlock-guard timeouts; no
// elapsed-time assertions (timing bounds flake on loaded CI runners).
public class FusionCacheFactoryRunnerTests
{
    private static readonly TimeSpan GuardTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task RunInScope_Gives_The_Factory_Its_Own_Scope_That_Survives_Caller_Scope_Disposal()
    {
        var testToken = TestContext.Current.CancellationToken;
        using var fixture = new RunnerFixture();
        var policy = RunnerFixture.CreatePolicy();

        ScopedDependency callerInstance;
        using (var callerScope = fixture.Services.CreateScope())
        {
            callerInstance = callerScope.ServiceProvider.GetRequiredService<ScopedDependency>();
        }

        callerInstance.Disposed.Should().BeTrue();

        var (factoryInstance, disposedWhileFactoryRan) = await fixture.Runner.RunInScope(
            policy,
            (services, _) =>
            {
                var instance = services.GetRequiredService<ScopedDependency>();
                return Task.FromResult((instance, instance.Disposed));
            },
            testToken);

        factoryInstance.Should().NotBeSameAs(callerInstance);
        disposedWhileFactoryRan.Should().BeFalse();
    }

    [Fact]
    public async Task RunInScope_Disposes_The_Factory_Scope_After_Completion_And_After_A_Throwing_Run()
    {
        var testToken = TestContext.Current.CancellationToken;
        using var fixture = new RunnerFixture();
        var policy = RunnerFixture.CreatePolicy();

        var completedInstance = await fixture.Runner.RunInScope(
            policy,
            (services, _) => Task.FromResult(services.GetRequiredService<ScopedDependency>()),
            testToken);
        completedInstance.Disposed.Should().BeTrue();

        ScopedDependency? throwingInstance = null;
        var act = async () => await fixture.Runner.RunInScope<int>(
            policy,
            (services, _) =>
            {
                throwingInstance = services.GetRequiredService<ScopedDependency>();
                throw new InvalidOperationException("factory failed");
            },
            testToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
        throwingInstance.Should().NotBeNull();
        throwingInstance.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task Cooperative_Deadline_Cancels_A_Token_Honoring_Factory()
    {
        var testToken = TestContext.Current.CancellationToken;
        using var fixture = new RunnerFixture();
        var policy = RunnerFixture.CreatePolicy(cancellationAfter: TimeSpan.FromMilliseconds(50), abandonAfter: TimeSpan.FromSeconds(8));
        var neverReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var act = async () => await fixture.Runner.RunInScope<int>(
            policy,
            async (_, ct) =>
            {
                await neverReleased.Task.WaitAsync(ct);
                return 1;
            },
            testToken);

        await act.Should().ThrowAsync<OperationCanceledException>();

        var entry = fixture.Logger.Entries.Should()
            .ContainSingle(x => x.EventId.Id == 2, "the cooperative deadline fired while the outer token did not")
            .Which;
        entry.Exception.Should().BeAssignableTo<OperationCanceledException>();
        testToken.IsCancellationRequested.Should().BeFalse();
        fixture.Logger.Entries.Should().NotContain(x => x.EventId.Id == 3);
    }

    [Fact]
    public async Task Token_Ignoring_Factory_Is_Abandoned_And_Its_Orphan_Cleans_Up_On_Completion()
    {
        var testToken = TestContext.Current.CancellationToken;
        using var fixture = new RunnerFixture();
        using var probe = new CounterProbe();
        var policy = RunnerFixture.CreatePolicy(cancellationAfter: TimeSpan.FromMilliseconds(50), abandonAfter: TimeSpan.FromMilliseconds(200));
        var ignoredGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ScopedDependency? factoryInstance = null;

        try
        {
            var act = async () => await fixture.Runner.RunInScope<int>(
                policy,
                async (services, _) =>
                {
                    factoryInstance = services.GetRequiredService<ScopedDependency>();
                    await ignoredGate.Task; // deliberately ignores the cancellation token
                    return 1;
                },
                testToken);

            await act.Should().ThrowAsync<OperationCanceledException>();

            var abandoned = fixture.Logger.Entries.Should().ContainSingle(x => x.EventId.Id == 3).Which;
            abandoned.Exception.Should().BeOfType<OperationCanceledException>();
            probe.Sum(policy.CacheName, CounterProbe.Orphans).Should().Be(1);
        }
        finally
        {
            ignoredGate.TrySetResult();
        }

        await WaitUntil(() => fixture.Logger.Entries.Any(x => x.EventId.Id == 4), testToken);
        factoryInstance.Should().NotBeNull();
        await WaitUntil(() => factoryInstance.Disposed, testToken);
        await WaitUntil(() => probe.Sum(policy.CacheName, CounterProbe.Executions) == 0, testToken);
        probe.Sum(policy.CacheName, CounterProbe.Orphans).Should().Be(0);
    }

    [Fact]
    public async Task Outer_Cancellation_Is_Rethrown_Without_Error_Logging_And_The_Orphan_Still_Cleans_Up()
    {
        var testToken = TestContext.Current.CancellationToken;
        using var fixture = new RunnerFixture();
        using var probe = new CounterProbe();
        var policy = RunnerFixture.CreatePolicy(cancellationAfter: TimeSpan.FromSeconds(8), abandonAfter: TimeSpan.FromSeconds(9));
        using var outerSource = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        var factoryEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ignoredGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ScopedDependency? factoryInstance = null;

        try
        {
            var invocation = fixture.Runner.RunInScope<int>(
                policy,
                async (services, _) =>
                {
                    factoryInstance = services.GetRequiredService<ScopedDependency>();
                    factoryEntered.TrySetResult();
                    await ignoredGate.Task; // deliberately ignores the cancellation token
                    return 1;
                },
                outerSource.Token);

            await factoryEntered.Task.WaitAsync(GuardTimeout, testToken);
            await outerSource.CancelAsync();

            var act = async () => await invocation;
            await act.Should().ThrowAsync<OperationCanceledException>();
            fixture.Logger.Entries.Should().NotContain(x => x.Level == Microsoft.Extensions.Logging.LogLevel.Error);
        }
        finally
        {
            ignoredGate.TrySetResult();
        }

        await WaitUntil(() => fixture.Logger.Entries.Any(x => x.EventId.Id == 7), testToken);
        factoryInstance.Should().NotBeNull();
        await WaitUntil(() => factoryInstance.Disposed, testToken);
        await WaitUntil(() => probe.Sum(policy.CacheName, CounterProbe.Executions) == 0, testToken);

        // The permit was released by the orphan's completion; a new invocation can enter.
        var result = await fixture.Runner.RunInScope(policy, (_, _) => Task.FromResult(42), testToken);
        result.Should().Be(42);
    }

    [Fact]
    public async Task Factory_Exception_Is_Logged_With_The_Exception_And_Rethrown()
    {
        var testToken = TestContext.Current.CancellationToken;
        using var fixture = new RunnerFixture();
        var policy = RunnerFixture.CreatePolicy();
        var failure = new InvalidOperationException("factory failed");

        var act = async () => await fixture.Runner.RunInScope<int>(policy, (_, _) => throw failure, testToken);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
        fixture.Logger.Entries.Should()
            .ContainSingle(x => x.EventId.Id == 1)
            .Which.Exception.Should().BeSameAs(failure);
    }

    [Fact]
    public async Task Concurrent_Executions_Are_Bounded_Before_Any_Scope_Opens_And_Recover_After_Release()
    {
        var testToken = TestContext.Current.CancellationToken;
        using var fixture = new RunnerFixture();
        using var probe = new CounterProbe();
        var policy = RunnerFixture.CreatePolicy(
            cancellationAfter: TimeSpan.FromMilliseconds(50),
            abandonAfter: TimeSpan.FromMilliseconds(200),
            maxConcurrentExecutions: 2);
        var ignoredGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var enteredCount = 0;

        try
        {
            // Permit acquisition happens synchronously during invocation, so invoking sequentially makes
            // admission deterministic: the first two acquire, the rest must be rejected without entering.
            var invocations = Enumerable.Range(0, 5)
                .Select(_ => fixture.Runner.RunInScope<int>(
                    policy,
                    async (services, _) =>
                    {
                        services.GetRequiredService<ScopedDependency>();
                        Interlocked.Increment(ref enteredCount);
                        await ignoredGate.Task; // deliberately ignores the cancellation token
                        return 1;
                    },
                    testToken))
                .ToList();

            var rejections = 0;
            var abandonments = 0;
            foreach (var invocation in invocations)
            {
                try
                {
                    await invocation.WaitAsync(GuardTimeout, testToken);
                }
                catch (FusionCacheFactoryRejectedException)
                {
                    rejections++;
                }
                catch (OperationCanceledException)
                {
                    abandonments++;
                }
            }

            rejections.Should().Be(3);
            abandonments.Should().Be(2);
            enteredCount.Should().Be(2);
            probe.Sum(policy.CacheName, CounterProbe.Executions).Should().Be(2, "abandoned executions keep their permits");
            probe.Sum(policy.CacheName, CounterProbe.Orphans).Should().Be(2);
            fixture.Logger.Entries.Count(x => x.EventId.Id == 5).Should().Be(1, "the exhausted interval is logged once, not per rejection");
            fixture.Logger.Entries.Should().NotContain(x => x.EventId.Id == 1, "rejections must not be logged as factory failures");
        }
        finally
        {
            ignoredGate.TrySetResult();
        }

        await WaitUntil(() => fixture.Logger.Entries.Count(x => x.EventId.Id == 4) == 2, testToken);
        await WaitUntil(() => probe.Sum(policy.CacheName, CounterProbe.Executions) == 0, testToken);
        probe.Sum(policy.CacheName, CounterProbe.Orphans).Should().Be(0);
        fixture.Logger.Entries.Count(x => x.EventId.Id == 6).Should().Be(1);

        var result = await fixture.Runner.RunInScope(policy, (_, _) => Task.FromResult(42), testToken);
        result.Should().Be(42);
    }

    [Fact]
    public async Task Abandonment_Releases_The_FusionCache_Key_Lock_And_A_Replacement_Factory_Can_Run()
    {
        const string key = "key";
        const string staleValue = "stale-value";
        var testToken = TestContext.Current.CancellationToken;
        using var fixture = new RunnerFixture();
        var policy = RunnerFixture.CreatePolicy(
            cancellationAfter: TimeSpan.FromMilliseconds(50),
            abandonAfter: TimeSpan.FromMilliseconds(250));

        using var cache = new FusionCache(Options.Create(new FusionCacheOptions { CacheName = policy.CacheName }));
        var options = new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMilliseconds(1),
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromMinutes(5),
            // Without this, fail-safe activation in phase a throttles phases b and c into serving stale
            // WITHOUT invoking the factory at all (default throttle is 30s).
            FailSafeThrottleDuration = TimeSpan.FromMilliseconds(1),
            FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
            // Generous headroom: tolerates the interval between EventId 3 and FusionCache's own lock release.
            FactoryHardTimeout = TimeSpan.FromSeconds(8)
        };

        var ignoredGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopesOpened = 0;
        var replacementRan = 0;

        // Seed fail-safe stock and let the 1ms Duration expire so every later GetOrSet triggers a refresh.
        await cache.SetAsync(key, staleValue, options, token: testToken);
        await Task.Delay(TimeSpan.FromMilliseconds(50), testToken);

        try
        {
            // Phase a: the first factory ignores cancellation; the caller is served stale at the soft
            // timeout while the detached completion runs on until the runner abandons it.
            var stalePhaseResult = await cache.GetOrSetAsync<string>(
                key,
                (_, ct) => fixture.Runner.RunInScope<string>(policy, async (services, _) =>
                {
                    services.GetRequiredService<ScopedDependency>();
                    Interlocked.Increment(ref scopesOpened);
                    await ignoredGate.Task; // deliberately ignores the cancellation token
                    return "never-produced";
                }, ct),
                options: options,
                token: testToken).AsTask().WaitAsync(GuardTimeout, testToken);
            stalePhaseResult.Should().Be(staleValue);

            // EventId 3 is a safe synchronization point: orphan registration precedes it.
            await WaitUntil(() => fixture.Logger.Entries.Any(x => x.EventId.Id == 3), testToken);

            // Phase b: the abandoned orphan still holds the single execution permit, so a refresh attempt
            // is rejected without opening a scope; the caller gets stale data instead of queueing behind
            // the (now released) per-key lock. EventId 3 is logged just before the abandonment exception
            // reaches FusionCache, so FusionCache's own lock release races this phase - and stale data with
            // an untouched scope count is also what a still-held key lock would produce. Retry until
            // FusionCache actually invokes the refresh delegate, which proves the key lock was acquired.
            var refreshDelegateEntered = 0;
            string? rejectedPhaseResult = null;
            using (var retryTimeout = CancellationTokenSource.CreateLinkedTokenSource(testToken))
            {
                retryTimeout.CancelAfter(GuardTimeout);
                while (Volatile.Read(ref refreshDelegateEntered) == 0)
                {
                    rejectedPhaseResult = await cache.GetOrSetAsync<string>(
                        key,
                        (_, ct) =>
                        {
                            Interlocked.Increment(ref refreshDelegateEntered);
                            return fixture.Runner.RunInScope<string>(policy, (services, _) =>
                            {
                                services.GetRequiredService<ScopedDependency>();
                                Interlocked.Increment(ref scopesOpened);
                                return Task.FromResult("never-produced-either");
                            }, ct);
                        },
                        options: options,
                        token: testToken).AsTask().WaitAsync(GuardTimeout, testToken);

                    if (Volatile.Read(ref refreshDelegateEntered) == 0)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(10), retryTimeout.Token);
                    }
                }
            }

            rejectedPhaseResult.Should().Be(staleValue);
            scopesOpened.Should().Be(1, "a rejected invocation must not open a scope");
            fixture.Logger.Entries.Should().Contain(x => x.EventId.Id == 5,
                "the refresh attempt must be a real capacity rejection, not stale data served behind a still-held key lock");
        }
        finally
        {
            ignoredGate.TrySetResult();
        }

        // Phase c: once the orphan completes and the permit returns, a replacement factory must be able to
        // acquire the same key and actually run.
        await WaitUntil(() => fixture.Logger.Entries.Any(x => x.EventId.Id == 6), testToken);

        var freshResult = await cache.GetOrSetAsync<string>(
            key,
            (_, ct) => fixture.Runner.RunInScope<string>(policy, (_, _) =>
            {
                Interlocked.Increment(ref replacementRan);
                return Task.FromResult("fresh-value");
            }, ct),
            options: options,
            token: testToken).AsTask().WaitAsync(GuardTimeout, testToken);

        replacementRan.Should().Be(1, "the replacement factory must actually run, not merely serve stale data");
        freshResult.Should().Be("fresh-value");
    }

    [Fact]
    public async Task A_Factory_That_Blocks_Before_Its_First_Await_Is_Still_Abandoned()
    {
        var testToken = TestContext.Current.CancellationToken;
        using var fixture = new RunnerFixture();
        var policy = RunnerFixture.CreatePolicy(cancellationAfter: TimeSpan.FromMilliseconds(50), abandonAfter: TimeSpan.FromMilliseconds(200));
        using var blocker = new ManualResetEventSlim(initialState: false);

        try
        {
            var act = async () => await fixture.Runner.Run(
                policy,
                _ =>
                {
                    blocker.Wait(GuardTimeout, CancellationToken.None); // blocks synchronously; never reaches an await
                    return Task.FromResult(1);
                },
                testToken);

            await act.Should().ThrowAsync<OperationCanceledException>();
            fixture.Logger.Entries.Should().Contain(x => x.EventId.Id == 3);
        }
        finally
        {
            blocker.Set();
        }
    }

    [Fact]
    public void Every_Policy_Orders_Its_Deadlines_And_Has_Valid_Capacity_And_Name()
    {
        var policies = FusionCacheFactoryPolicy.All;

        policies.Should().NotBeEmpty();
        policies.Select(x => x.CacheName).Should().OnlyHaveUniqueItems();
        foreach (var policy in policies)
        {
            policy.CacheName.Should().NotBeNullOrWhiteSpace();
            policy.MaxConcurrentExecutions.Should().BePositive();
            policy.HardTimeout.Should().BePositive();
            policy.HardTimeout.Should().BeLessThan(policy.CancellationAfter,
                "waiters must be released (or served stale) before cooperative cancellation starts");
            policy.CancellationAfter.Should().BeLessThan(policy.AbandonAfter,
                "cooperative cancellation must get a chance before hard abandonment");
        }
    }

    [Fact]
    public void Every_Declared_Policy_Is_Included_In_All()
    {
        // All is the hand-maintained inventory that drives both the invariant test above and the runner's
        // startup metric registration; a policy declared on the type but missing from All silently escapes both.
        var declared = typeof(FusionCacheFactoryPolicy)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(x => x.FieldType == typeof(FusionCacheFactoryPolicy))
            .ToList();

        declared.Should().NotBeEmpty();
        foreach (var field in declared)
        {
            var policy = (FusionCacheFactoryPolicy)field.GetValue(null)!;
            FusionCacheFactoryPolicy.All.Should().Contain(policy, $"the declared policy '{field.Name}' must be part of the inventory");
        }

        FusionCacheFactoryPolicy.All.Should().HaveCount(declared.Count);
    }

    [Fact]
    public async Task A_Throwing_Scope_Disposal_Still_Releases_The_Execution_Permit()
    {
        var testToken = TestContext.Current.CancellationToken;
        using var fixture = new RunnerFixture();
        using var probe = new CounterProbe();
        var policy = RunnerFixture.CreatePolicy();

        var act = async () => await fixture.Runner.RunInScope(
            policy,
            (services, _) =>
            {
                services.GetRequiredService<ThrowingAsyncDisposable>();
                return Task.FromResult(1);
            },
            testToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
        fixture.Logger.Entries.Should().ContainSingle(x => x.EventId.Id == 1);
        probe.Sum(policy.CacheName, CounterProbe.Executions).Should().Be(0);

        var result = await fixture.Runner.RunInScope(policy, (_, _) => Task.FromResult(42), testToken);
        result.Should().Be(42);
    }

    private static async Task WaitUntil(Func<bool> condition, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(GuardTimeout);
        while (!condition())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
        }
    }

    private sealed class RunnerFixture : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        public RunnerFixture()
        {
            _serviceProvider = new ServiceCollection()
                .AddScoped<ScopedDependency>()
                .AddScoped<ThrowingAsyncDisposable>()
                .BuildServiceProvider();
            Logger = new CollectingLogger<FusionCacheFactoryRunner>();
            Runner = new FusionCacheFactoryRunner(_serviceProvider.GetRequiredService<IServiceScopeFactory>(), Logger);
        }

        public IServiceProvider Services => _serviceProvider;
        public CollectingLogger<FusionCacheFactoryRunner> Logger { get; }
        public FusionCacheFactoryRunner Runner { get; }

        public static FusionCacheFactoryPolicy CreatePolicy(
            TimeSpan? cancellationAfter = null,
            TimeSpan? abandonAfter = null,
            int maxConcurrentExecutions = 1) => new()
            {
                // Unique per test: the metric instruments are process-global, so isolation comes from the tag.
                CacheName = $"test-{Guid.NewGuid():N}",
                HardTimeout = TimeSpan.FromMilliseconds(10),
                CancellationAfter = cancellationAfter ?? TimeSpan.FromSeconds(8),
                AbandonAfter = abandonAfter ?? TimeSpan.FromSeconds(9),
                MaxConcurrentExecutions = maxConcurrentExecutions
            };

        public void Dispose() => _serviceProvider.Dispose();
    }

    private sealed class ScopedDependency : IDisposable
    {
        public bool Disposed { get; private set; }
        public bool DisposedDuringUse => Disposed;
        public void Dispose() => Disposed = true;
    }

    private sealed class ThrowingAsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => throw new InvalidOperationException("scope disposal failed");
    }

    /// <summary>
    /// Sums the process-global runner counters per cache-name tag. Tests must filter by their own unique
    /// cache name and never assert global totals (parallel tests share the instruments).
    /// </summary>
    private sealed class CounterProbe : IDisposable
    {
        public const string Executions = "dialogporten.fusioncache.factory_executions_active";
        public const string Orphans = "dialogporten.fusioncache.factory_orphans_active";

        private readonly MeterListener _listener = new();
        private readonly Lock _lock = new();
        private readonly Dictionary<(string CacheName, string Instrument), long> _sums = [];

        public CounterProbe()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == FusionCacheFactoryTelemetry.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            {
                foreach (var tag in tags)
                {
                    if (tag.Key != "cache.name" || tag.Value is not string cacheName)
                    {
                        continue;
                    }

                    lock (_lock)
                    {
                        var key = (cacheName, instrument.Name);
                        _sums[key] = _sums.GetValueOrDefault(key) + measurement;
                    }

                    return;
                }
            });
            _listener.Start();
        }

        public long Sum(string cacheName, string instrument)
        {
            lock (_lock)
            {
                return _sums.GetValueOrDefault((cacheName, instrument));
            }
        }

        public void Dispose() => _listener.Dispose();
    }
}
