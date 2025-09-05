namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

internal static class EnumeratorExtensions
{
    public static T GetNext<T>(this IEnumerator<T> enumerator)
    {
        return !enumerator.MoveNext()
            ? throw new InvalidOperationException("Enumerator has no more elements.")
            : enumerator.Current;
    }
}
