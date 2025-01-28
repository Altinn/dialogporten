using MediatR;

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
    [Obsolete($"Use {nameof(OccurredAt)} instead")]
    DateTimeOffset OccuredAt { get; set; }

    /// <summary>
    /// The time at which the event, as well as all the actual changes, occurred.
    /// </summary>
    DateTimeOffset OccurredAt
    {
        // Will be removed once all events in the database/on the bus have been consumed
#pragma warning disable CS0618 // Type or member is obsolete
        get => OccuredAt;
        set => OccuredAt = value;
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Dictionary of metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }
}
