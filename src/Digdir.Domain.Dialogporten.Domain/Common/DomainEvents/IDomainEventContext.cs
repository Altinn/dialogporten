namespace Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;

public interface IDomainEventContext
{
    Dictionary<string, string> Metadata { get; }
    void AddMetadata(string key, string value);
}
