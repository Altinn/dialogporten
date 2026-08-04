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
}
