using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using Mediator;

namespace Digdir.Domain.Dialogporten.Application.Common;

public static class ApplicationEventHandlerUtils
{
    public static HandlerEventMapping[] GetHandlerEventMaps()
    {
        var openNotificationHandler = typeof(INotificationHandler<>);
        var domainEventType = typeof(IDomainEvent);
        return ApplicationAssemblyMarker.Assembly.DefinedTypes
            .Where(x => x is { IsInterface: false, IsAbstract: false })
            .SelectMany(x => x
                .GetInterfaces()
                .Where(@interface =>
                    @interface.IsGenericType
                    && @interface.GetGenericTypeDefinition() == openNotificationHandler
                    && @interface.GetGenericArguments().Single().IsAssignableTo(domainEventType))
                .Select(@interface => (@class: x, @event: @interface.GetGenericArguments().Single()))
                .Select(x => new HandlerEventMapping(x.@class.AsType(), x.@event, EndpointNameAttribute.GetName(x.@class, x.@event))))
            .ToArray();
    }
}

public record struct HandlerEventMapping(Type HandlerType, Type EventType, string EndpointName);
