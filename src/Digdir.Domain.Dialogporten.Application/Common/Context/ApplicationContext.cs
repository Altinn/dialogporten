namespace Digdir.Domain.Dialogporten.Application.Common.Context;

public sealed class ApplicationContext : IApplicationContext
{
    public Dictionary<string, string> Metadata { get; } = [];
    public void AddMetadata(string key, string value) => Metadata[key] = value;
}
