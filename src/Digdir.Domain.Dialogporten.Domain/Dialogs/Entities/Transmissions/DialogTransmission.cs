using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

public sealed class DialogTransmission :
    IImmutableEntity,
    IIdentifiableEntity,
    ICreatableEntity,
    IAggregateCreatedHandler,
    IEventPublisher,
    IAuthorizationContextCarrier
{
    public Guid Id { get; set; }
    public string? IdempotentKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The legacy authorization attribute this transmission is authorized by. A transmission governed by an
    /// <see cref="AuthorizationContext"/> stores a rollback sentinel here instead of anything the service
    /// owner supplied, so that code predating authorization contexts keeps the transmission hidden. Read it
    /// through <see cref="EffectiveLegacyAuthorizationAttribute"/> rather than comparing to the sentinel.
    /// </summary>
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// <see cref="AuthorizationAttribute"/> as API consumers should see it, i.e. the legacy attribute to
    /// authorize by, or null when this transmission is governed by an <see cref="AuthorizationContext"/> and
    /// the stored value is therefore the rollback sentinel. Keyed on the context rather than on the
    /// sentinel's value, because that value is also a well-formed attribute a service owner may supply on
    /// its own: suppressing it there would hide a real restriction and drop it on a GET → PUT round trip.
    /// </summary>
    public string? EffectiveLegacyAuthorizationAttribute =>
        AuthorizationContext is null ? AuthorizationAttribute : null;

    public Uri? ExtendedType { get; set; }
    public string? ExternalReference { get; set; }

    // === Principal relationships ===
    [AggregateChild]
    public List<DialogTransmissionContent> Content { get; set; } = [];

    [AggregateChild]
    public List<DialogTransmissionAttachment> Attachments { get; set; } = [];

    [AggregateChild]
    public List<DialogTransmissionNavigationalAction> NavigationalActions { get; set; } = [];

    [AggregateChild]
    public DialogTransmissionSenderActor Sender { get; set; } = null!;

    [AggregateChild]
    public DialogTransmissionAuthorizationContext? AuthorizationContext { get; set; }

    AuthorizationContext? IAuthorizationContextCarrier.AuthorizationContext => AuthorizationContext;

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
    {
        _domainEvents.Add(new DialogTransmissionCreatedDomainEvent(
            DialogId,
            Id,
            Sender.ActorTypeId,
            Dialog.ServiceResource,
            Dialog.Party,
            Dialog.Process,
            Dialog.PrecedingProcess));

        if (Dialog.VisibleFrom is { } visibleFrom && visibleFrom > utcNow)
        {
            CreatedAt = visibleFrom;
        }
    }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IEnumerable<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
    public bool HasEvents() => _domainEvents.Count != 0;
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
