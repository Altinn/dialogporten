using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using MassTransit;
using Mediator;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

internal sealed class TestDomainEventConsumer<T> : IConsumer<T>
    where T : class, IDomainEvent
{
    private readonly IPublisher _publisher;

    public TestDomainEventConsumer(IPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public async Task Consume(ConsumeContext<T> context) => await _publisher.Publish(context.Message);
}
