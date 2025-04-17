using System.Collections;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;

/// <summary>
/// Represents a key-value store for data loaded by <see cref="IDataLoader{TRequest,TResponse}"/> implementations.
/// </summary>
internal interface IDataLoaderContext : IEnumerable<KeyValuePair<string, object?>>
{
    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <returns>The value associated with the specified key, throws <see cref="KeyNotFoundException"/> if the key does not exist.</returns>
    /// <exception cref="KeyNotFoundException">When <paramref name="key"/> not found.</exception>
    object? Get(string key);
    /// <summary>
    /// Sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to set.</param>
    /// <param name="value">The value to set.</param>
    void Set(string key, object? value);
}

/// <inheritdoc/>
internal sealed class DataLoaderContext : IDataLoaderContext
{
    private readonly Dictionary<string, object?> _data = [];

    /// <inheritdoc/>
    public object? Get(string key) => _data[key];

    /// <inheritdoc/>
    public void Set(string key, object? value) => _data[key] = value;

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _data.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
