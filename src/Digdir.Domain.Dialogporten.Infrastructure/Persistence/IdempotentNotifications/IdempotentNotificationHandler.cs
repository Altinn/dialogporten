using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using Mediator;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.IdempotentNotifications;

internal sealed class IdempotentNotificationHandler<TNotification> :
    INotificationHandler<TNotification>,
    // We need to manually register this NotificationHandler because
    // it should decorate all INotificationHandler<TNotification>
    // instances, not be a notification handler in of itself.
    IIgnoreOnAssemblyScan
    where TNotification : IDomainEvent
{
    private readonly INotificationHandler<TNotification> _decorated;
    private readonly INotificationProcessingContextFactory _processingContextFactory;

    public IdempotentNotificationHandler(INotificationHandler<TNotification> decorated, INotificationProcessingContextFactory processingContextFactory)
    {
        _decorated = decorated ?? throw new ArgumentNullException(nameof(decorated));
        _processingContextFactory = processingContextFactory ?? throw new ArgumentNullException(nameof(processingContextFactory));
    }

    public async ValueTask Handle(TNotification notification, CancellationToken cancellationToken)
    {
        var handlerName = _decorated.GetType().FullName ?? throw new InvalidOperationException("Could not determine the handler name.");
        var transaction = _processingContextFactory.GetExistingContext(notification.EventId);
        if (await transaction.HandlerIsAcked(handlerName, cancellationToken))
        {
            // I've handled this event before, so I'm not going to do it again.
            return;
        }

        await _decorated.Handle(notification, cancellationToken);
        await transaction.AckHandler(handlerName, cancellationToken);
    }
}
