using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;

public sealed record Conflict(string PropertyName, string ErrorMessage)
{
    public static Conflict Empty { get; } = new(string.Empty, string.Empty);

    public List<ValidationFailure> ToValidationResults() =>
        string.IsNullOrWhiteSpace(PropertyName) || string.IsNullOrWhiteSpace(ErrorMessage)
            ? []
            : [new(PropertyName, ErrorMessage)];
}
