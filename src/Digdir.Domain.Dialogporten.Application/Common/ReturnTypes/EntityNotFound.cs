using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;

public sealed record EntityNotFound<T>(IEnumerable<object> Keys) : EntityNotFound(typeof(T).Name, Keys)
{
    public EntityNotFound(IEnumerable<Guid> keys) : this(keys.Cast<object>()) { }
    public EntityNotFound(Guid key) : this(new object[] { key }) { }
}

public record EntityNotFound(string Name, IEnumerable<object> Keys)
{
    public string Message => $"Entity '{Name}' with the following key(s) was not found: ({string.Join(", ", Keys)}).";
    public EntityNotFound(string name, IEnumerable<Guid> keys) : this(name, keys.Cast<object>()) { }
    public EntityNotFound(string name, Guid key) : this(name, new object[] { key }) { }

    public List<ValidationFailure> ToValidationResults() => [new(Name, Message)];
}
