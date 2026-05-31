using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;

public sealed record EntityExpired<T>(DateTimeOffset ExpiredAt)
    : EntityExpired(typeof(T).Name, ExpiredAt);

public record EntityExpired(string Name, DateTimeOffset ExpiredAt)
{
    public string Message => $"Dialog expired at {ExpiredAt:s}";

    public List<ValidationFailure> ToValidationResults() => [new(Name, Message)];
}
