using Polly;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class E2ERetryPolicies
{
    private const string DefaultWarningMessage = "The operation took longer than expected.";
    private const string E2EWarningTag = "[E2E_WARNING]";

    public static async Task<T> RetryUntilAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> isSuccessful,
        CancellationToken? cancellationToken = null,
        TimeSpan? delay = null,
        TimeSpan? warningAfter = null,
        TimeSpan? failAfter = null,
        string warningMessage = DefaultWarningMessage,
        [CallerMemberName] string callerMemberName = "")
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(isSuccessful);

        cancellationToken ??= TestContext.Current.CancellationToken;

        var retryDelay = delay ?? TimeSpan.FromSeconds(1);
        var warningThreshold = warningAfter ?? TimeSpan.FromSeconds(5);
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

        ArgumentOutOfRangeException.ThrowIfLessThan(retryDelay, TimeSpan.FromSeconds(1));

        var elapsed = Stopwatch.StartNew();
        var warningLogged = false;

        var policy = Policy<T>
            .Handle<Exception>(ex => ex is not OperationCanceledException)
            .OrResult(result => !isSuccessful(result))
            .WaitAndRetryAsync(
                int.MaxValue,
                _ => retryDelay,
                (_, _, _, _) =>
                {
                    var elapsedTime = elapsed.Elapsed;

                    if (!warningLogged && elapsedTime >= warningThreshold)
                    {
                        warningLogged = true;
                        TestContext.Current.AddWarning(
                            $"{E2EWarningTag} {callerMemberName}: {warningMessage} " +
                            $"Elapsed time: {elapsedTime:hh\\:mm\\:ss\\.fff}");
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
            : throw new TimeoutException(
                $"The operation did not succeed within the allowed time window ({failThreshold}).");
    }
}
