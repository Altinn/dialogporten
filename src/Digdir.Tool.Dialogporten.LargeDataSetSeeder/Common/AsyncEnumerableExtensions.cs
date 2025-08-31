namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> WIthTaskCompletionSource<T>(this IAsyncEnumerable<T> values, TaskCompletionSource finished)
    {
        await foreach (var value in values)
        {
            yield return value;
            if (finished.Task.IsCompleted) yield break;
        }
    }
}
