using System.Diagnostics;

namespace Digdir.Domain.Dialogporten.Domain.Common.Exceptions;

public static class ComparableExtensions
{
    /// <summary>
    /// Returns <paramref name="value"/> constrained to the inclusive range
    /// [<paramref name="min"/>, <paramref name="max"/>].
    /// </summary>
    public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T>
    {
        Debug.Assert(min.CompareTo(max) <= 0, "min must be <= max");
        return value.CompareTo(min) < 0 ? min
            : value.CompareTo(max) > 0 ? max
            : value;
    }
}
