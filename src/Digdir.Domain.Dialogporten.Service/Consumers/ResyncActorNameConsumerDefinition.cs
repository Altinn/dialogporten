using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.ResyncActorName;
using Digdir.Domain.Dialogporten.Domain.Actors.Events;
using MassTransit;

namespace Digdir.Domain.Dialogporten.Service.Consumers;

public sealed class ResyncActorNameConsumerDefinition : ConsumerDefinition<DomainEventConsumer<ResyncActorName, ResyncActorNameEvent>>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<DomainEventConsumer<ResyncActorName, ResyncActorNameEvent>> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseDelayedRedelivery(r => r.Intervals(
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromHours(2),
            TimeSpan.FromHours(24),
            TimeSpan.FromHours(24),
            TimeSpan.FromHours(24)));
        endpointConfigurator.DiscardFaultedMessages();
    }
}
