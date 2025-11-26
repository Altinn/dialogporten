using System.Diagnostics.CodeAnalysis;

namespace Digdir.Domain.Dialogporten.Infrastructure;

internal static class IEnumerableExtensions
{
    internal static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? enumerable) =>
        enumerable is null || !enumerable.Any();
}
