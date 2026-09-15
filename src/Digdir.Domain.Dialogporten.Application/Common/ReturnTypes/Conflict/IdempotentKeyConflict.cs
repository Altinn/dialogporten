namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Conflict;

public sealed record IdempotentKeyConflict(params List<string> ConflictingIdempotentKeys)
    : IConflictAttemptedValue;
