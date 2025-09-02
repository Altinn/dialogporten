namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

internal static class EnumeratorExtensions
{
    public static T GetNext<T>(this IEnumerator<T> enumerator)
    {
        return !enumerator.MoveNext()
            ? throw new InvalidOperationException("Enumerator has no more elements.")
            : enumerator.Current;
    }

    public static async IAsyncEnumerable<T> WithTaskCompletionSource<T>(this IAsyncEnumerable<T> values, TaskCompletionSource finished)
    {
        await foreach (var value in values)
        {
            yield return value;
            if (finished.Task.IsCompleted) yield break;
        }
    }
}
