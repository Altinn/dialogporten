using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Interceptors;

internal sealed class DisabledFastLaneDomainEventPublisher : IFastLaneDomainEventPublisher
{
    public Task<bool> TryPublishAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        => Task.FromResult(false);
}
