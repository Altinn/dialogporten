using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;

public sealed class DialogServiceOwnerLabel : IImmutableEntity
{
    public const int MaxNumberOfLabels = 20;

    private string _value = null!;
    public Guid Id { get; set; }

    public string Value
    {
        get => _value;
        set => _value = value.Trim().ToLowerInvariant();
    }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid DialogServiceOwnerContextId { get; set; }
    public DialogServiceOwnerContext DialogServiceOwnerContext { get; set; } = null!;
}
