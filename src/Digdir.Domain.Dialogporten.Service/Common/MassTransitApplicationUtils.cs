using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Service.Consumers;
using MassTransit;

namespace Digdir.Domain.Dialogporten.Service.Common;

internal static class MassTransitApplicationUtils
{
    public static ApplicationConsumerMapping[] GetApplicationConsumerMaps()
    {
        var openDomainEventConsumer = typeof(DomainEventConsumer<,>);
        var openDomainEventConsumerDefinition = typeof(DomainEventConsumerDefinition<,>);
        var consumerDefinitionTypeByConsumerType = GetConsumerDefinitionTypeByConsumerType();
        return ApplicationEventHandlerUtils
            .GetHandlerEventMaps()
            .Select(x =>
            {
                var busConsumerType = openDomainEventConsumer.MakeGenericType(x.HandlerType, x.EventType);
                return new ApplicationConsumerMapping(
                    AppConsumerType: x.HandlerType,
                    BusConsumerType: busConsumerType,
                    BusDefinitionType: consumerDefinitionTypeByConsumerType.GetValueOrDefault(busConsumerType)
                        ?? openDomainEventConsumerDefinition.MakeGenericType(x.HandlerType, x.EventType),
                    EndpointName: x.EndpointName);
            })
            .ToArray();
    }

    // Custom definitions are concrete types inheriting ConsumerDefinition<DomainEventConsumer<THandler, TEvent>>.
    // They take priority over the default DomainEventConsumerDefinition for their handler.
    private static Dictionary<Type, Type> GetConsumerDefinitionTypeByConsumerType()
    {
        var openDomainEventConsumer = typeof(DomainEventConsumer<,>);
        var openConsumerDefinition = typeof(ConsumerDefinition<>);
        return ServiceAssemblyMarker.Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Select(t => (Definition: t, Base: t.BaseType))
            .Where(x => x.Base is { IsGenericType: true } b
                        && b.GetGenericTypeDefinition() == openConsumerDefinition
                        && b.GetGenericArguments()[0] is { IsGenericType: true } c
                        && c.GetGenericTypeDefinition() == openDomainEventConsumer)
            .ToDictionary(x => x.Base!.GetGenericArguments()[0], x => x.Definition);
    }
}

internal record struct ApplicationConsumerMapping(Type AppConsumerType, Type BusConsumerType, Type BusDefinitionType, string EndpointName);
