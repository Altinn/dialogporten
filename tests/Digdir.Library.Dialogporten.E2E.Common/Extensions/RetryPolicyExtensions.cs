using Polly;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class RetryPolicyExtensions
{
    public static async Task<T> RetryUntilAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> isSuccessful,
        CancellationToken cancellationToken,
        int maxAttempts = 10,
        TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(isSuccessful);

        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        var policy = Policy<T>
            .HandleResult(result => !isSuccessful(result))
            .WaitAndRetryAsync(maxAttempts - 1, _ => delay ?? TimeSpan.FromSeconds(2));

        return await policy.ExecuteAsync(operation, cancellationToken);
    }
}
