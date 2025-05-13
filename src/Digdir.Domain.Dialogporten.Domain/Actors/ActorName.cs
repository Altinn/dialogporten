using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.Actors;

public sealed class ActorName : IImmutableEntity, IIdentifiableEntity, ICreatableEntity
{
    public Guid Id { get; set; }
    public string? ActorId { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<Actor> ActorEntities { get; set; } = [];
}
