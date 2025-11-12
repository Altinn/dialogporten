using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
public sealed record EntityNotVisible<T>(DateTimeOffset VisibleFrom) : EntityNotVisible(typeof(T).Name, VisibleFrom);
public record EntityNotVisible(string Name, DateTimeOffset VisibleFrom)
{
    public string Message => $"Dialog is not visible until {VisibleFrom:s}";

    public List<ValidationFailure> ToValidationResults() => [new(Name, Message)];
}
