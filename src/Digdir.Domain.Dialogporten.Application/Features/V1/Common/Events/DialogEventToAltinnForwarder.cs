using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Library.Entity.Abstractions.Features.EventPublisher;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events;

internal sealed class DialogEventToAltinnForwarder : DomainEventToAltinnForwarderBase,
    INotificationHandler<DialogCreatedDomainEvent>,
    INotificationHandler<DialogUpdatedDomainEvent>,
    INotificationHandler<DialogDeletedDomainEvent>,
    INotificationHandler<DialogSeenDomainEvent>
{
    public DialogEventToAltinnForwarder(ICloudEventBus cloudEventBus, IDialogDbContext db,
        IConfiguration configuration)
        : base(cloudEventBus, db, configuration) { }

    public async Task Handle(DialogCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var dialog = await GetDialog(domainEvent.DialogId, cancellationToken);
        var cloudEvent = CreateCloudEvent(domainEvent, dialog);
        await CloudEventBus.Publish(cloudEvent, cancellationToken);
    }

    public async Task Handle(DialogUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var dialog = await GetDialog(domainEvent.DialogId, cancellationToken);
        var cloudEvent = CreateCloudEvent(domainEvent, dialog);
        await CloudEventBus.Publish(cloudEvent, cancellationToken);
    }

    public async Task Handle(DialogSeenDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var dialog = await GetDialog(domainEvent.DialogId, cancellationToken);
        var cloudEvent = CreateCloudEvent(domainEvent, dialog);
        await CloudEventBus.Publish(cloudEvent, cancellationToken);
    }

    public async Task Handle(DialogDeletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var cloudEvent = new CloudEvent
        {
            Id = domainEvent.EventId,
            Type = CloudEventTypes.Get(domainEvent),
            Time = domainEvent.OccuredAt,
            Resource = domainEvent.ServiceResource,
            ResourceInstance = domainEvent.DialogId.ToString(),
            Subject = domainEvent.Party,
            Source = $"{DialogportenBaseUrl()}/api/v1/enduser/dialogs/{domainEvent.DialogId}"
        };

        await CloudEventBus.Publish(cloudEvent, cancellationToken);
    }

    private CloudEvent CreateCloudEvent(IDomainEvent domainEvent, DialogEntity dialog, Dictionary<string, object>? data = null) => new()
    {
        Id = domainEvent.EventId,
        Type = CloudEventTypes.Get(domainEvent),
        Time = domainEvent.OccuredAt,
        Resource = dialog.ServiceResource,
        ResourceInstance = dialog.Id.ToString(),
        Subject = dialog.Party,
        Source = $"{DialogportenBaseUrl()}/api/v1/enduser/dialogs/{dialog.Id}",
        Data = data
    };
}
