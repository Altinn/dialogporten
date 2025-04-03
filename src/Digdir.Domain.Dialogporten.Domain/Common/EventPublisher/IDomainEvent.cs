using Mediator;

namespace Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;

/// <summary>
/// Represents a domain event.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// The events' identification. Consumers can use this to ensure exactly once processing.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// The time at which the event, as well as all the actual changes, occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; set; }

    /// <summary>
    /// Dictionary of metadata.
    /// </summary>
    Dictionary<string, string> Metadata { get; set; }
}
