using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;

public sealed record Forbidden(List<string> Reasons)
{
    private const string ForbiddenMessage = "Forbidden";

    public Forbidden(params string[] reasons) : this(reasons.ToList()) { }

    public List<ValidationFailure> ToValidationResults() =>
        [.. Reasons.Select(x => new ValidationFailure(ForbiddenMessage, x))];

    public Forbidden WithInvalidDialogIds(List<Guid> dialogIds)
    {
        Reasons.Add($"The following dialog ids are unauthorized and/or missing: ({string.Join(", ", dialogIds)}).");
        return this;
    }
}
