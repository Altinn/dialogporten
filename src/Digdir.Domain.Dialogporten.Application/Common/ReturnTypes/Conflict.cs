using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;

public sealed record Conflict(IEnumerable<ConflictError> Conflicts)
{
    public Conflict(ConflictError error) : this([error]) { }
    public List<ValidationFailure> ToValidationResults() => Conflicts
        .Select(x => new ValidationFailure(x.PropertyName, x.ErrorMessage))
        .ToList();
}

public sealed record ConflictError(string PropertyName, string ErrorMessage);
