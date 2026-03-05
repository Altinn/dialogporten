using Polly;
using System.Diagnostics;
using Xunit;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class RetryPolicyExtensions
{
    public static async Task<T> RetryUntilAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> isSuccessful,
        CancellationToken? cancellationToken = null,
        int maxAttempts = int.MaxValue,
        TimeSpan? delay = null,
        TimeSpan? warningAfter = null,
        TimeSpan? failAfter = null,
        Action<TimeSpan>? onWarning = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(isSuccessful);

        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        cancellationToken ??= TestContext.Current.CancellationToken;

        var retryDelay = delay ?? TimeSpan.FromSeconds(2);
        var warningThreshold = warningAfter ?? TimeSpan.FromSeconds(4);
        var failThreshold = failAfter ?? TimeSpan.FromSeconds(10);

        if (warningThreshold < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(warningAfter),
                "warningAfter must be greater than or equal to 00:00:00.");
        }

        if (failThreshold < warningThreshold)
        {
            throw new ArgumentOutOfRangeException(nameof(failAfter),
                "failAfter must be greater than or equal to warningAfter.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(retryDelay, TimeSpan.FromMilliseconds(1));

        var elapsed = Stopwatch.StartNew();
        var warningLogged = false;
        var maxRetries = maxAttempts - 1;

        var policy = Policy<T>
            .HandleResult(result => !isSuccessful(result))
            .WaitAndRetryAsync(
                maxRetries,
                _ => retryDelay,
                (_, _, _, _) =>
                {
                    var elapsedTime = elapsed.Elapsed;

                    if (!warningLogged && elapsedTime >= warningThreshold)
                    {
                        warningLogged = true;
                        onWarning?.Invoke(elapsedTime);
                    }

                    if (elapsedTime >= failThreshold)
                    {
                        throw new TimeoutException(
                            $"The operation did not succeed within the allowed threshold ({failThreshold}). " +
                            $"Elapsed: {elapsedTime}.");
                    }
                });

        var result = await policy.ExecuteAsync(operation, cancellationToken.Value);

        return isSuccessful(result)
            ? result
            : throw new TimeoutException($"The operation did not succeed within the allowed number of attempts ({{maxAttempts}}).");
    }
}
