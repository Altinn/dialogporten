namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;

internal interface IDataLoaderContext
{
    object? Get(string key);
    void Set(string key, object? value);
}

internal sealed class DataLoaderContext : IDataLoaderContext
{
    private readonly Dictionary<string, object?> _data = [];
    public object? Get(string key) => _data[key];
    public void Set(string key, object? value) => _data[key] = value;
}
