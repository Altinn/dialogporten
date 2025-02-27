namespace Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;

/// <summary>
/// Abstraction representing functionality to publish domain events.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Get a collection of <see cref="IDomainEvent"/> queued up for dispatching at the end of the next unit of work.
    /// This also clears the collection on the EventPublisher.
    /// </summary>
    IEnumerable<IDomainEvent> PopDomainEvents();
}
