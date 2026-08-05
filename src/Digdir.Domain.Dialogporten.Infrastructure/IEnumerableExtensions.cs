using System.Diagnostics.CodeAnalysis;

namespace Digdir.Domain.Dialogporten.Infrastructure;

internal static class IEnumerableExtensions
{
    internal static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? enumerable) =>
        enumerable is null || !enumerable.Any();

    /// <summary>
    /// Drops null/blank entries and removes duplicates using <paramref name="comparer"/>. Used to normalize
    /// inbound party-identifier filters before lookup/caching; callers materialize and order as needed (the
    /// comparer is caller-specified because lookup and cache-key contexts intentionally differ).
    /// </summary>
    internal static IEnumerable<string> NormalizeParties(this IEnumerable<string> parties, StringComparer comparer) =>
        parties.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(comparer);

    public static IEnumerable<IEnumerable<T>> Permutations<T>(this IEnumerable<T> items, int? length = null)
    {
        var list = items.ToList();
        length ??= list.Count;

        if (length == 0)
        {
            yield return Enumerable.Empty<T>();
            yield break;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var current = list[i];
            var remaining = list.Where((_, index) => index != i);

            foreach (var permutation in remaining.Permutations(length - 1))
            {
                yield return new[] { current }.Concat(permutation);
            }
        }
    }
}

