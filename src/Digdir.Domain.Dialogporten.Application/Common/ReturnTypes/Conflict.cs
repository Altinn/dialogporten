using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Conflicts;
using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;

public sealed record Conflict(string PropertyName, string ErrorMessage, IConflictReason Reason)
{
    public List<ValidationFailure> ToValidationResults()
    {
        var validationFailure = new ValidationFailure(PropertyName, ErrorMessage)
        {
            CustomState = Reason
        };

        return [validationFailure];
    }
}
