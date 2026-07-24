using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

// Pins the FusionCache semantics the service-resource cache configuration relies on: while one caller's factory
// holds the per-key memory lock, a concurrent caller with fail-safe enabled and a stale value available must be
// SERVED THE STALE VALUE at FactorySoftTimeout instead of queueing behind the in-flight factory. The cache
// registrations in InfrastructureExtensions depend on this to keep requests fast whenever a rebuild is slow or
// failing; if a FusionCache upgrade changes the behavior, this test fails loudly.
//
// Scope: this pins third-party library semantics only, not the production registration values (those live in
// object initializers inside a private method and are intentionally not exposed for testing).
public class FusionCacheFailSafeSemanticsTests
{
    [Fact]
    public async Task Concurrent_Caller_Gets_Stale_Value_At_Soft_Timeout_While_Factory_Holds_The_Key_Lock()
    {
        const string key = "key";
        const string staleValue = "stale-value";
        var timeout = TimeSpan.FromSeconds(10);
        var testToken = TestContext.Current.CancellationToken;

        using var cache = new FusionCache(Options.Create(new FusionCacheOptions { CacheName = "semantics-test" }));
        var options = new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMilliseconds(1),
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromMinutes(5),
            FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
            FactoryHardTimeout = TimeSpan.FromMinutes(1)
        };

        var factoryEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactory = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await cache.SetAsync(key, staleValue, options, token: testToken);
        // Let the 1ms Duration expire so the next GetOrSet triggers a refresh with fail-safe stock available.
        await Task.Delay(TimeSpan.FromMilliseconds(50), testToken);

        var refreshHoldingTheLock = cache.GetOrSetAsync<string>(
            key,
            async (_, ct) =>
            {
                factoryEntered.TrySetResult();
                await releaseFactory.Task.WaitAsync(ct);
                return "fresh-value";
            },
            options: options,
            token: testToken).AsTask();

        try
        {
            // Only proceed once the factory genuinely holds the per-key lock.
            await factoryEntered.Task.WaitAsync(timeout, testToken);

            var concurrentResult = await cache.GetOrSetAsync<string>(
                    key,
                    (_, _) => throw new InvalidOperationException(
                        "The concurrent caller must be served the stale value, not run its own factory."),
                    options: options,
                    token: testToken)
                .AsTask()
                .WaitAsync(timeout, testToken);

            concurrentResult.Should().Be(staleValue);
        }
        finally
        {
            releaseFactory.TrySetResult();
            await refreshHoldingTheLock.WaitAsync(timeout, testToken);
        }
    }

    [Fact]
    public async Task Concurrent_Caller_Queues_On_The_Key_Lock_When_No_Fail_Safe_Value_Exists()
    {
        // The hazard side of the semantics: without a stale value to fall back on, a concurrent caller has
        // nothing to be served and waits on the per-key memory lock for as long as the in-flight factory runs.
        // In production this is the state where requests hang until the request timeout kills them; the cache
        // configuration mitigates it by keeping fail-safe stock alive far longer than any expected failure
        // streak, so this state is only reachable on a cold key.
        const string key = "key";
        var timeout = TimeSpan.FromSeconds(10);
        var testToken = TestContext.Current.CancellationToken;

        using var cache = new FusionCache(Options.Create(new FusionCacheOptions { CacheName = "semantics-test" }));
        var options = new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMinutes(5),
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromMinutes(5),
            FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
            FactoryHardTimeout = TimeSpan.FromMinutes(1)
        };

        var factoryEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactory = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // No initial Set: the key is cold, so there is no fail-safe value to serve.
        var buildHoldingTheLock = cache.GetOrSetAsync<string>(
            key,
            async (_, ct) =>
            {
                factoryEntered.TrySetResult();
                await releaseFactory.Task.WaitAsync(ct);
                return "fresh-value";
            },
            options: options,
            token: testToken).AsTask();

        try
        {
            await factoryEntered.Task.WaitAsync(timeout, testToken);

            var concurrentCall = cache.GetOrSetAsync<string>(
                key,
                (_, _) => throw new InvalidOperationException(
                    "The queued caller must wait for the in-flight factory's value, not run its own factory."),
                options: options,
                token: testToken).AsTask();

            // The concurrent caller must still be waiting well past the soft timeout: with no fallback value,
            // the soft timeout cannot serve it and the lock wait has no configured limit.
            await Task.Delay(TimeSpan.FromMilliseconds(800), testToken);
            concurrentCall.IsCompleted.Should().BeFalse();

            // Once the factory completes, the queued caller is served its result without running a factory.
            releaseFactory.TrySetResult();
            var concurrentResult = await concurrentCall.WaitAsync(timeout, testToken);
            concurrentResult.Should().Be("fresh-value");
        }
        finally
        {
            releaseFactory.TrySetResult();
            await buildHoldingTheLock.WaitAsync(timeout, testToken);
        }
    }
}
