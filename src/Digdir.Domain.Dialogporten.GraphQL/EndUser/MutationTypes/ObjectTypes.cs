using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;

public sealed class SetSystemLabelPayload
{
    public bool Success { get; set; }
    public List<ISetSystemLabelError> Errors { get; set; } = [];
}

public sealed class SetSystemLabelInput
{
    public Guid DialogId { get; set; }
    public List<SystemLabel> SystemLabels { get; set; } = [];
}

[InterfaceType("SetSystemLabelError")]
public interface ISetSystemLabelError
{
    string Message { get; set; }
}

public sealed class SetSystemLabelEntityNotFound : ISetSystemLabelError
{
    public string Message { get; set; } = null!;
}

public sealed class SetSystemLabelForbidden : ISetSystemLabelError
{
    public string Message { get; set; } = null!;
}

public sealed class SetSystemLabelDomainError : ISetSystemLabelError
{
    public string Message { get; set; } = null!;
}

public sealed class SetSystemLabelConcurrencyError : ISetSystemLabelError
{
    public string Message { get; set; } = null!;
}

public sealed class SetSystemLabelEntityDeleted : ISetSystemLabelError
{
    public string Message { get; set; } = null!;
}

public sealed class SetSystemLabelValidationError : ISetSystemLabelError
{
    public string Message { get; set; } = null!;
}
