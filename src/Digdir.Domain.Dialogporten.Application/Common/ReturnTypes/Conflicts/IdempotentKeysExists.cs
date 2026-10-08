namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Conflicts;

public sealed class IdempotentKeysExist : IConflictReason
{
    public required List<string> IdempotentKeys { get; init; }
}
