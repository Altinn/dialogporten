using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;

public sealed class DialogServiceOwnerLabel : IImmutableEntity, ICreatableEntity
{
    public const int MaxNumberOfLabels = 20;

    public string Value
    {
        get;
        set => field = value.Trim().ToLowerInvariant();
    } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid DialogServiceOwnerContextId { get; set; }
    public DialogServiceOwnerContext DialogServiceOwnerContext { get; set; } = null!;
}
