using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;

public sealed record Conflict(string PropertyName, string ErrorMessage)
{
    public List<ValidationFailure> ToValidationResults() => [new(PropertyName, ErrorMessage)];
}
