using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Digdir.Library.Entity.Abstractions;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;
using Digdir.Library.Entity.Abstractions.Features.SoftDeletable;
using Digdir.Library.Entity.Abstractions.Features.Versionable;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

public sealed class DialogEntity :
    IEntity,
    ISoftDeletableEntity,
    IVersionableEntity,
    IAggregateChangedHandler,
    IEventPublisher,
    IAggregateRestoredHandler
{
    public Guid Id { get; set; }
    public Guid Revision { get; set; }
    public string? IdempotentKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset ContentUpdatedAt { get; set; }
    public bool Deleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string Org { get; set; } = null!;
    public string ServiceResource { get; set; } = null!;
    public string ServiceResourceType { get; set; } = null!;
    public string Party { get; set; } = null!;
    public int? Progress { get; set; }
    public string? ExtendedStatus { get; set; }
    public string? ExternalReference { get; set; }
    public DateTimeOffset? VisibleFrom { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public string? Process { get; set; }

    public string? PrecedingProcess { get; set; }

    /// <summary>
    /// Indicates if this dialog is intended for API consumption only and should not be displayed in user interfaces.
    /// </summary>
    public bool IsApiOnly { get; set; }

    /// <summary>
    /// Indicates whether the dialog contains content that has not been viewed or opened by the user yet.
    /// </summary>
    public bool HasUnopenedContent { get; set; }


    // === Dependent relationships ===
    public DialogStatus.Values StatusId { get; set; }

    public DialogStatus Status { get; set; } = null!;

    // === Principal relationships ===
    [AggregateChild]
    public List<DialogTransmission> Transmissions { get; set; } = [];

    [AggregateChild]
    public List<DialogContent> Content { get; set; } = [];

    [AggregateChild]
    public List<DialogSearchTag> SearchTags { get; set; } = [];

    [AggregateChild]
    public List<DialogAttachment> Attachments { get; set; } = [];

    [AggregateChild]
    public List<DialogGuiAction> GuiActions { get; set; } = [];

    [AggregateChild]
    public List<DialogApiAction> ApiActions { get; set; } = [];

    [AggregateChild]
    public List<DialogActivity> Activities { get; set; } = [];

    [AggregateChild]
    public List<DialogSeenLog> SeenLog { get; set; } = [];

    public DialogEndUserContext EndUserContext { get; set; } = null!;
    public DialogServiceOwnerContext ServiceOwnerContext { get; set; } = null!;

    public void OnCreate(AggregateNode self, DateTimeOffset utcNow)
    {
        _domainEvents.Add(new DialogCreatedDomainEvent(Id, ServiceResource, Party, Process, PrecedingProcess));
        ContentUpdatedAt = utcNow;
    }

    public void OnUpdate(AggregateNode self, DateTimeOffset utcNow)
    {
        _domainEvents.Add(new DialogUpdatedDomainEvent(Id, ServiceResource, Party, Process, PrecedingProcess));

        if (ContentHasChanged(self))
        {
            ContentUpdatedAt = utcNow;
        }
    }

    private static bool ContentHasChanged(AggregateNode self)
    {
        var childrenChanged = self.Children.Any(x =>
            x.Entity is
                DialogTransmission or
                DialogContent or
                DialogAttachment or
                DialogGuiAction or
                DialogApiAction);

        var propertiesChanged = self.ModifiedProperties.Any(x =>
            x.PropertyName is nameof(ExtendedStatus) or nameof(StatusId));

        return childrenChanged || propertiesChanged;
    }

    public void OnDelete(AggregateNode self, DateTimeOffset utcNow)
        => _domainEvents.Add(new DialogDeletedDomainEvent(Id, ServiceResource, Party, Process, PrecedingProcess));

    public void OnRestore(AggregateNode self, DateTimeOffset utcNow)
        => _domainEvents.Add(new DialogRestoredDomainEvent(Id, ServiceResource, Party, Process, PrecedingProcess));

    public void UpdateSeenAt(string endUserId, DialogUserType.Values userTypeId, string? endUserName)
    {
        var lastSeenAt = SeenLog
                         .Where(x => x.SeenBy.ActorNameEntity?.ActorId == endUserId)
                         .MaxBy(x => x.CreatedAt)
                         ?.CreatedAt
         ?? DateTimeOffset.MinValue;

        if (lastSeenAt >= UpdatedAt)
        {
            return;
        }

        SeenLog.Add(new DialogSeenLog
        {
            EndUserTypeId = userTypeId,
            IsViaServiceOwner = userTypeId == DialogUserType.Values.ServiceOwnerOnBehalfOfPerson,
            SeenBy = new DialogSeenLogSeenByActor
            {
                ActorTypeId = ActorType.Values.PartyRepresentative,
                ActorNameEntity = new ActorName
                {
                    Name = endUserName,
                    ActorId = endUserId
                }
            }
        });
        _domainEvents.Add(new DialogSeenDomainEvent(Id, ServiceResource, Party, Process, PrecedingProcess));
    }

    private readonly List<IDomainEvent> _domainEvents = [];

    public IEnumerable<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
}

public sealed class DialogAttachment : Attachment
{
    public Guid DialogId { get; set; }
    public DialogEntity Dialog { get; set; } = null!;
}
