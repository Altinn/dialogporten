using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using MassTransit;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

internal sealed class TestDomainEventConsumer<T> : IConsumer<T>
    where T : class, IDomainEvent
{
    private readonly IPublisher _publisher;

    public TestDomainEventConsumer(IPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<T> context) => await _publisher.Publish(context.Message);
}
