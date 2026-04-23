using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;

namespace Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;

/// <summary>
/// Marks domain events that may be published directly to the broker when no database writes are pending.
/// </summary>
public interface IFastLaneDomainEvent : IDomainEvent;
