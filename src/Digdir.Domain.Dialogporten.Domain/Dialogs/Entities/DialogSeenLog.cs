using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

public sealed class DialogSeenLog : IImmutableEntity, IIdentifiableEntity, ICreatableEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsViaServiceOwner { get; set; }

    // === Principal relationships ===
    [AggregateChild]
    public DialogSeenLogSeenByActor SeenBy { get; set; } = null!;

    // === Dependent relationships ===
    public Guid DialogId { get; set; }
    public DialogEntity Dialog { get; set; } = null!;

    public DialogUserType.Values EndUserTypeId { get; set; }
    public DialogUserType EndUserType { get; set; } = null!;
}

public sealed class DialogSeenLogSeenByActor : Actor, IImmutableEntity
{
    public Guid DialogSeenLogId { get; set; }
    public DialogSeenLog DialogSeenLog { get; set; } = null!;
}
