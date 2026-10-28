using System.ComponentModel.DataAnnotations.Schema;
using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.Actors;

public sealed class ActorName : IImmutableEntity, IIdentifiableEntity, ICreatableEntity, IEventPublisher
{
    public Guid Id { get; set; }
    public string? ActorId { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<Actor> ActorEntities { get; set; } = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    public IEnumerable<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
    public bool HasEvents() => _domainEvents.Count != 0;

    [NotMapped]
    public bool NeedsActorNameResync => ActorId is not null && Name is null;

    public void RequestActorNameResync(string reason)
    {
        if (!NeedsActorNameResync) return;
        this.EnsureId();
        _domainEvents.Add(new ResyncActorNameEvent(Id, reason));
    }
}
