using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Conflict;

public sealed record Conflict(string PropertyName, string ErrorMessage, IConflictAttemptedValue? AttemptedValues = null)
{
    public List<ValidationFailure> ToValidationResults() => [new(PropertyName, ErrorMessage, AttemptedValues)];
}
