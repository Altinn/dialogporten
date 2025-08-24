using System.Diagnostics.CodeAnalysis;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

[SuppressMessage("Style", "IDE0048:Add parentheses for clarity")]
public static class ExponentialDistribution
{
    /// <summary>
    /// Generates a random index based on an exponential distribution.
    /// </summary>
    /// <param name="arraySize">The size of the array to generate an index for.</param>
    /// <param name="lambda">The rate parameter of the exponential distribution. Higher values result in a steeper distribution.</param>
    /// <param name="rng">An optional random number generator. If null, a default instance will be used.</param>
    /// <returns>A random index based on the exponential distribution.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="arraySize"/> or <paramref name="lambda"/> is less than or equal to zero.</exception>
    public static int NextExponentialIndex(int arraySize, double lambda = 0.005, Random? rng = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(arraySize, nameof(arraySize));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lambda, nameof(lambda));
        return NextExponentialIndex(lambda, MaxCdf(arraySize, lambda), rng ?? Random.Shared);
    }

    /// <summary>
    /// Generates a sequence of random indices based on an exponential distribution.
    /// </summary>
    /// <param name="count">The number of indices to generate.</param>
    /// <param name="arraySize">The size of the array to generate indices for.</param>
    /// <param name="lambda">The rate parameter of the exponential distribution. Higher values result in a steeper distribution.</param>
    /// <param name="rng">An optional random number generator. If null, a default instance will be used.</param>
    /// <returns>An enumerable sequence of random indices based on the exponential distribution.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/>, <paramref name="arraySize"/>, or <paramref name="lambda"/> is less than or equal to zero.</exception>
    /// <remarks>
    /// The indices are generated such that they follow an exponential distribution, which means that lower indices are more likely to be chosen than higher indices.
    /// The distribution is controlled by the <paramref name="lambda"/> parameter, where higher values result in a steeper distribution.
    /// </remarks>
    public static IEnumerable<int> NextExponentialIndices(int count, int arraySize, double lambda = 0.005, Random? rng = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count, nameof(count));
        using var expEnumerable = NextExponentialIndicesForever(arraySize, lambda, rng).GetEnumerator();
        for (var i = 0; i < count; i++)
        {
            yield return expEnumerable.EnumerateOrThrow();
        }
    }

    /// <summary>
    /// Generates a sequence of random indices based on an exponential distribution, with a base distribution every N indices.
    /// </summary>
    /// <param name="count">The total number of indices to generate.</param>
    /// <param name="minDistribution">The number of base indices to generate.</param>
    /// <param name="arraySize">The size of the array to generate indices for.</param>
    /// <param name="lambda">The rate parameter of the exponential distribution. Higher values result in a steeper distribution.</param>
    /// <param name="rng">An optional random number generator. If null, a default instance will be used.</param>
    /// <returns>An enumerable sequence of random indices based on the exponential distribution, with a base distribution every N indices.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="count"/>, <paramref name="minDistribution"/>, or <paramref name="arraySize"/> is less than or equal to zero,
    /// or if <paramref name="minDistribution"/> is greater than <paramref name="count"/> divided by <paramref name="arraySize"/>.
    /// </exception>
    /// <remarks>
    /// This method generates a sequence of indices where every N-th index is taken from a base distribution,
    /// while the rest are generated based on an exponential distribution. The base distribution is evenly spaced throughout the sequence.
    /// The <paramref name="lambda"/> parameter controls the steepness of the exponential distribution,
    /// where higher values result in a steeper distribution favoring lower indices.
    /// </remarks>
    public static IEnumerable<int> NextExponentialIndicesWithBase(int count, int minDistribution, int arraySize, double lambda = 0.005, Random? rng = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count, nameof(count));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minDistribution, count / arraySize, nameof(minDistribution));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minDistribution, nameof(minDistribution));
        var distributeEveryNth = count / (arraySize * minDistribution);
        var distributionIndex = 0;
        using var expEnumerable = NextExponentialIndicesForever(arraySize, lambda, rng).GetEnumerator();
        for (var i = 0; i < count; i++)
        {
            yield return i % distributeEveryNth == 0
                ? distributionIndex++ % arraySize
                : expEnumerable.EnumerateOrThrow();
        }
    }

    private static IEnumerable<int> NextExponentialIndicesForever(int arraySize, double lambda = 0.005, Random? rng = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(arraySize, nameof(arraySize));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lambda, nameof(lambda));
        var maxCdf = MaxCdf(arraySize, lambda);
        rng ??= Random.Shared;
        while (true)
        {
            yield return NextExponentialIndex(lambda, maxCdf, rng);
        }
        // ReSharper disable once IteratorNeverReturns
    }

    private static int NextExponentialIndex(double lambda, double maxCdf, Random rng) =>
        (int)Math.Floor(-Math.Log(1 - rng.NextDouble() * maxCdf) / lambda);

    private static double MaxCdf(int arraySize, double lambda) =>
        1 - Math.Exp(-lambda * arraySize);

    private static int EnumerateOrThrow(this IEnumerator<int> enumerator)
    {
        if (!enumerator.MoveNext())
            throw new InvalidOperationException("Enumerator has no more elements.");
        return enumerator.Current;
    }
}
