using Polly;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class E2ERetryPolicies
{
    private const string DefaultDegradationMessage = "The operation took longer than expected.";
    private const string E2EWarningTag = "[E2E_WARNING]";

    public static async Task<T> RetryUntilAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> isSuccessful,
        CancellationToken? cancellationToken = null,
        TimeSpan? delay = null,
        TimeSpan? logWarningAfter = null,
        TimeSpan? failAfter = null,
        string degradationMessage = DefaultDegradationMessage,
        Func<Exception, bool>? exceptionFilter = null,
        [CallerMemberName] string callerMemberName = "")
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(isSuccessful);

        cancellationToken ??= TestContext.Current.CancellationToken;

        var retryDelay = delay ?? TimeSpan.FromSeconds(1);
        var warningThreshold = logWarningAfter ?? TimeSpan.FromSeconds(10);
        var failThreshold = failAfter ?? TimeSpan.FromSeconds(20);

        if (warningThreshold < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(logWarningAfter),
                "logWarningAfter must be greater than or equal to 00:00:00.");
        }

        if (failThreshold < warningThreshold)
        {
            throw new ArgumentOutOfRangeException(nameof(failAfter),
                "failAfter must be greater than or equal to logWarningAfter.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(retryDelay, TimeSpan.FromSeconds(1));

        var elapsed = Stopwatch.StartNew();
        var warningLogged = false;

        var policy = Policy<T>
            .Handle(exceptionFilter ?? DefaultExceptionHandler)
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
                            $"{E2EWarningTag} {callerMemberName}: {degradationMessage} " +
                            $"Elapsed time: {elapsedTime:hh\\:mm\\:ss\\.fff}");
                    }

                    if (elapsedTime >= failThreshold)
                    {
                        throw new TimeoutException(
                            $"The operation did not succeed within the allowed threshold ({failThreshold}). " +
                            $"Elapsed: {elapsedTime}.");
                    }
                });

        return await policy.ExecuteAsync(operation, cancellationToken.Value);
    }

    private static bool DefaultExceptionHandler(Exception ex) =>
        ex is not OperationCanceledException;
}
