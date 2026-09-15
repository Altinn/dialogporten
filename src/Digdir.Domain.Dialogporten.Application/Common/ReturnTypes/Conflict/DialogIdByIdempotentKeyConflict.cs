namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Conflict;

public sealed record DialogIdByIdempotentKeyConflict(string SuppliedIdempotentKey, Guid ConflictingDialogId)
    : IConflictAttemptedValue;
