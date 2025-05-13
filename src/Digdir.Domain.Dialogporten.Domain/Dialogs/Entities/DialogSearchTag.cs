using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

public sealed class DialogSearchTag : IImmutableTimestampedEntity
{
    private string _value = null!;
    public Guid Id { get; set; }

    public string Value
    {
        get => _value;
        set => _value = value.Trim().ToLowerInvariant();
    }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid DialogId { get; set; }
    public DialogEntity Dialog { get; set; } = null!;
}
