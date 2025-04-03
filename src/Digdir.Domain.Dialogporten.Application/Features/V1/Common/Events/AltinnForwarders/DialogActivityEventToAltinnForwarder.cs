using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events.Activities;
using Mediator;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.AltinnForwarders;

internal sealed class DialogActivityEventToAltinnForwarder : DomainEventToAltinnForwarderBase,
    INotificationHandler<DialogActivityCreatedDomainEvent>
{
    public DialogActivityEventToAltinnForwarder(ICloudEventBus cloudEventBus, IOptions<ApplicationSettings> settings)
        : base(cloudEventBus, settings) { }

    [EndpointName("DialogEventToAltinnForwarder_DialogActivityCreatedDomainEvent")]
    public async ValueTask Handle(DialogActivityCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.ShouldNotBeSentToAltinnEvents())
        {
            return;
        }

        var cloudEvent = new CloudEvent
        {
            Id = domainEvent.EventId,
            Type = CloudEventTypes.Get(domainEvent.TypeId.ToString()),
            Time = domainEvent.OccurredAt,
            Resource = domainEvent.ServiceResource,
            ResourceInstance = domainEvent.DialogId.ToString(),
            Subject = domainEvent.Party,
            Source = $"{SourceBaseUrl()}{domainEvent.DialogId}/activities/{domainEvent.ActivityId}",
            Data = GetCloudEventData(domainEvent)
        };

        await CloudEventBus.Publish(cloudEvent, cancellationToken);
    }

    private static Dictionary<string, object> GetCloudEventData(DialogActivityCreatedDomainEvent domainEvent)
    {
        var data = new Dictionary<string, object>
        {
            ["activityId"] = domainEvent.ActivityId.ToString()
        };

        if (domainEvent.ExtendedType is not null)
        {
            data["extendedActivityType"] = domainEvent.ExtendedType.ToString();
        }

        if (domainEvent.Process is not null)
        {
            data["process"] = domainEvent.Process;
        }
        if (domainEvent.PrecedingProcess is not null)
        {
            data["precedingProcess"] = domainEvent.PrecedingProcess;
        }
        return data;
    }
}
