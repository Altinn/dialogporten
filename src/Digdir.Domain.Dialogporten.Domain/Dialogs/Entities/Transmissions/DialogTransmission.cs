using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;
using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

public sealed class DialogTransmission :
    IImmutableEntity,
    IAggregateCreatedHandler,
    IEventPublisher
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? AuthorizationAttribute { get; set; }
    public Uri? ExtendedType { get; set; }

    // === Principal relationships ===
    [AggregateChild]
    public List<DialogTransmissionContent> Content { get; set; } = [];

    [AggregateChild]
    public List<DialogTransmissionAttachment> Attachments { get; set; } = [];

    [AggregateChild]
    public DialogTransmissionSenderActor Sender { get; set; } = null!;

    public List<DialogTransmission> RelatedTransmissions { get; set; } = [];

    public List<DialogActivity> Activities { get; set; } = [];

    // === Dependent relationships ===
    public Guid DialogId { get; set; }
    public DialogEntity Dialog { get; set; } = null!;

    public Guid? RelatedTransmissionId { get; set; }
    public DialogTransmission? RelatedTransmission { get; set; }

    public DialogTransmissionType.Values TypeId { get; set; }
    public DialogTransmissionType Type { get; set; } = null!;

    public void OnCreate(AggregateNode self, DateTimeOffset utcNow)
        => _domainEvents.Add(new DialogTransmissionCreatedDomainEvent(
            DialogId,
            Id,
            Dialog.ServiceResource,
            Dialog.Party,
            Dialog.Process,
            Dialog.PrecedingProcess));

    private readonly List<IDomainEvent> _domainEvents = [];
    public IEnumerable<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
}

public sealed class DialogTransmissionSenderActor : Actor, IImmutableEntity
{
    public Guid TransmissionId { get; set; }
    public DialogTransmission Transmission { get; set; } = null!;
}

public sealed class DialogTransmissionAttachment : Attachment, IImmutableEntity
{
    public Guid TransmissionId { get; set; }
    public DialogTransmission Transmission { get; set; } = null!;
}
