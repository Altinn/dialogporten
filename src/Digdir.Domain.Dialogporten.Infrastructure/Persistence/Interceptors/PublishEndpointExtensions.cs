using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using MassTransit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Interceptors;

internal static class PublishEndpointExtensions
{
    extension(IPublishEndpoint publishEndpoint)
    {
        /// <summary>
        /// Publishes a domain event by its runtime type. Events that ask for a
        /// <see cref="IDelayedDomainEvent.DeliveryDelay"/> are handed to the transport with that delay,
        /// which the outbox stores as the message's enqueue time.
        /// </summary>
        public Task PublishDomainEvent(IDomainEvent domainEvent, CancellationToken cancellationToken) => domainEvent switch
        {
            IDelayedDomainEvent { DeliveryDelay: { } delay } => publishEndpoint.Publish(
                domainEvent,
                domainEvent.GetType(),
                context => context.Delay = delay,
                cancellationToken),
            _ => publishEndpoint.Publish(domainEvent, domainEvent.GetType(), cancellationToken)
        };
    }
}
