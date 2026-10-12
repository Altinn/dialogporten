namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Conflicts;

public sealed class DialogIdForIdempotentKeyExists : IConflictReason
{
    public required string IdempotentKey { get; init; }
    public required Guid DialogId { get; init; }
}
