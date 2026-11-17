using System.Text.Json.Serialization;

namespace Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;

/// <summary>
/// A domain event that may ask the publishing infrastructure to hold back its first delivery attempt.
/// </summary>
public interface IDelayedDomainEvent : IDomainEvent
{
    /// <summary>
    /// How long to wait before the event is first delivered. <c>null</c> means deliver as soon as possible.
    /// This is a publishing hint, not part of the event contract.
    /// </summary>
    TimeSpan? DeliveryDelay { get; }
}
