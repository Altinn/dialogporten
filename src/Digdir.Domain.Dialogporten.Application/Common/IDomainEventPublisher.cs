using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;

namespace Digdir.Domain.Dialogporten.Application.Common;

/// <summary>
/// Publishes domain events that are not owned by any tracked entity. Published events are
/// dispatched together with entity events at the end of the next unit of work.
/// </summary>
public interface IDomainEventPublisher : IEventPublisher
{
    void Publish(IDomainEvent domainEvent);
}

internal sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public void Publish(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public IEnumerable<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }

    public bool HasEvents() => _domainEvents.Count != 0;
}
