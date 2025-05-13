using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

public sealed class LabelAssignmentLog : IImmutableTimestampedEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Name { get; set; } = null!;
    public string Action { get; set; } = null!;

    public Guid ContextId { get; set; }
    public DialogEndUserContext Context { get; set; } = null!;

    public LabelAssignmentLogActor PerformedBy { get; set; } = null!;
}

public sealed class LabelAssignmentLogActor : Actor, IImmutableTimestampedEntity
{
    public Guid LabelAssignmentLogId { get; set; }
    public LabelAssignmentLog LabelAssignmentLog { get; set; } = null!;
}
