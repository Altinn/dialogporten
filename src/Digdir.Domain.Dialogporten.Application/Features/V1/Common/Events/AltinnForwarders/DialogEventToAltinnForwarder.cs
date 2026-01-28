using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Mediator;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.AltinnForwarders;

internal sealed class DialogEventToAltinnForwarder : DomainEventToAltinnForwarderBase,
    INotificationHandler<DialogTransmissionCreatedDomainEvent>,
    INotificationHandler<DialogCreatedDomainEvent>,
    INotificationHandler<DialogUpdatedDomainEvent>,
    INotificationHandler<DialogDeletedDomainEvent>,
    INotificationHandler<DialogRestoredDomainEvent>,
    INotificationHandler<DialogSeenDomainEvent>
{
    public DialogEventToAltinnForwarder(ICloudEventBus cloudEventBus, IOptions<ApplicationSettings> settings)
        : base(cloudEventBus, settings) { }

    [EndpointName("DialogEventToAltinnForwarder_DialogCreatedDomainEvent")]
    public async ValueTask Handle(DialogCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.ShouldNotBeSentToAltinnEvents())
        {
            return;
        }

        var cloudEvent = new CloudEvent
        {
            Id = domainEvent.EventId,
            Type = CloudEventTypes.Get(nameof(DialogCreatedDomainEvent)),
            Time = domainEvent.OccurredAt,
            Resource = domainEvent.ServiceResource,
            ResourceInstance = domainEvent.DialogId.ToString(),
            Subject = domainEvent.Party,
            Source = $"{SourceBaseUrl()}{domainEvent.DialogId}",
            Data = GetCloudEventData(domainEvent)
        };

        await CloudEventBus.Publish(cloudEvent, cancellationToken);
    }

    [EndpointName("DialogEventToAltinnForwarder_DialogUpdatedDomainEvent")]
    public async ValueTask Handle(DialogUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.ShouldNotBeSentToAltinnEvents())
        {
            return;
        }

        var cloudEvent = new CloudEvent
        {
            Id = domainEvent.EventId,
            Type = CloudEventTypes.Get(nameof(DialogUpdatedDomainEvent)),
            Time = domainEvent.OccurredAt,
            Resource = domainEvent.ServiceResource,
            ResourceInstance = domainEvent.DialogId.ToString(),
            Subject = domainEvent.Party,
            Source = $"{SourceBaseUrl()}{domainEvent.DialogId}",
            Data = GetCloudEventData(domainEvent)
        };

        await CloudEventBus.Publish(cloudEvent, cancellationToken);
    }

    [EndpointName("DialogEventToAltinnForwarder_DialogSeenDomainEvent")]
    public async ValueTask Handle(DialogSeenDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.ShouldNotBeSentToAltinnEvents())
        {
            return;
        }

        var cloudEvent = new CloudEvent
        {
            Id = domainEvent.EventId,
            Type = CloudEventTypes.Get(nameof(DialogSeenDomainEvent)),
            Time = domainEvent.OccurredAt,
            Resource = domainEvent.ServiceResource,
            ResourceInstance = domainEvent.DialogId.ToString(),
            Subject = domainEvent.Party,
            Source = $"{SourceBaseUrl()}{domainEvent.DialogId}",
            Data = GetCloudEventData(domainEvent)
        };

        await CloudEventBus.Publish(cloudEvent, cancellationToken);
    }

    [EndpointName("DialogEventToAltinnForwarder_DialogDeletedDomainEvent")]
    public async ValueTask Handle(DialogDeletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.ShouldNotBeSentToAltinnEvents())
        {
            return;
        }

        var cloudEvent = new CloudEvent
        {
            Id = domainEvent.EventId,
            Type = CloudEventTypes.Get(nameof(DialogDeletedDomainEvent)),
            Time = domainEvent.OccurredAt,
            Resource = domainEvent.ServiceResource,
            ResourceInstance = domainEvent.DialogId.ToString(),
            Subject = domainEvent.Party,
            Source = $"{SourceBaseUrl()}{domainEvent.DialogId}",
            Data = GetCloudEventData(domainEvent)
        };

        await CloudEventBus.Publish(cloudEvent, cancellationToken);
    }

    [EndpointName("DialogEventToAltinnForwarder_DialogRestoredDomainEvent")]
    public async ValueTask Handle(DialogRestoredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.ShouldNotBeSentToAltinnEvents())
        {
            return;
        }

        var cloudEvent = new CloudEvent
        {
            Id = domainEvent.EventId,
            Type = CloudEventTypes.Get(nameof(DialogRestoredDomainEvent)),
            Time = domainEvent.OccurredAt,
            Resource = domainEvent.ServiceResource,
            ResourceInstance = domainEvent.DialogId.ToString(),
            Subject = domainEvent.Party,
            Source = $"{SourceBaseUrl()}{domainEvent.DialogId}",
            Data = GetCloudEventData(domainEvent)
        };

        await CloudEventBus.Publish(cloudEvent, cancellationToken);
    }

    [EndpointName("DialogEventToAltinnForwarder_DialogTransmissionCreatedDomainEvent")]
    public async ValueTask Handle(DialogTransmissionCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.ShouldNotBeSentToAltinnEvents())
        {
            return;
        }

        var cloudEvent = new CloudEvent
        {
            Id = domainEvent.EventId,
            Type = CloudEventTypes.Get(nameof(DialogTransmissionCreatedDomainEvent)),
            Time = domainEvent.OccurredAt,
            Resource = domainEvent.ServiceResource,
            ResourceInstance = domainEvent.DialogId.ToString(),
            Subject = domainEvent.Party,
            Source = $"{SourceBaseUrl()}{domainEvent.DialogId}/transmissions/{domainEvent.TransmissionId}",
            Data = GetCloudEventData(domainEvent)
        };

        await CloudEventBus.Publish(cloudEvent, cancellationToken);
    }

    private static Dictionary<string, object>? GetCloudEventData(IProcessEvent domainEvent)
    {
        var data = new Dictionary<string, object>();

        if (domainEvent.Process is not null)
        {
            data["process"] = domainEvent.Process;
        }
        if (domainEvent.PrecedingProcess is not null)
        {
            data["precedingProcess"] = domainEvent.PrecedingProcess;
        }

        return data.Count == 0 ? null : data;
    }

}
