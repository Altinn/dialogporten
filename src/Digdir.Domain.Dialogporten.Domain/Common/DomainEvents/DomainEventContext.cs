namespace Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;

public sealed class DomainEventContext : IDomainEventContext
{
    public Dictionary<string, string> Metadata { get; } = [];
    public void AddMetadata(string key, string value) => Metadata[key] = value;
}
