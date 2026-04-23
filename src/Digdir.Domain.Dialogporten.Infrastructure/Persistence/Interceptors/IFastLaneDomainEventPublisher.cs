using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Interceptors;

internal interface IFastLaneDomainEventPublisher
{
    Task<bool> TryPublishAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}
