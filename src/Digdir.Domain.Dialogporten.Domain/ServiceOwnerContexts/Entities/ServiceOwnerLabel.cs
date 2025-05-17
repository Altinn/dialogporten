using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.ServiceOwnerContexts.Entities;

public sealed class ServiceOwnerLabel : IImmutableEntity
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

    public Guid ServiceOwnerContextId { get; set; }
    public ServiceOwnerContext ServiceOwnerContext { get; set; } = null!;
}
