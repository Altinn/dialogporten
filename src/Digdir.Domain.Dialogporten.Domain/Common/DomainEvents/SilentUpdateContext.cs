namespace Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;

public sealed class SilentUpdateContext : ISilentUpdateContext
{
    public Dictionary<string, string> Metadata { get; } = [];
    public void AddMetadata(string key, string value) => Metadata[key] = value;
}
