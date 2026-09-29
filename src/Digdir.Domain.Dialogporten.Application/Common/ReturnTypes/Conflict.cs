using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.ErrorReasons;
using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;

public sealed record Conflict(string PropertyName, string ErrorMessage, ConflictErrorReason[] Reasons)
{
    public List<ValidationFailure> ToValidationResults()
    {
        var validationFailure = new ValidationFailure(PropertyName, ErrorMessage)
        {
            CustomState = this
        };

        return [validationFailure];
    }
}
