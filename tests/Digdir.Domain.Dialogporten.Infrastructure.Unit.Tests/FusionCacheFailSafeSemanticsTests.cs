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

            // Receiving the stale value at all proves the caller did not wait for the factory: the factory
            // cannot produce "fresh-value" until releaseFactory is set in the finally block below. No
            // elapsed-time assertion is needed (or wanted: timing bounds flake on loaded CI runners); the
            // WaitAsync above only guards against the semantics changing into an indefinite lock wait.
            concurrentResult.Should().Be(staleValue);
        }
        finally
        {
            releaseFactory.TrySetResult();
            await refreshHoldingTheLock.WaitAsync(timeout, testToken);
        }
    }
}
