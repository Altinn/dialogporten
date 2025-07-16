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

public sealed class BulkSetSystemLabelInput
{
    public List<DialogRevisionInput> Dialogs { get; set; } = [];
    public List<SystemLabel> SystemLabels { get; set; } = [];
}

public sealed class DialogRevisionInput
{
    public Guid DialogId { get; set; }
    public Guid? EnduserContextRevision { get; set; }
}

public sealed class BulkSetSystemLabelPayload
{
    public bool Success { get; set; }
    public List<IBulkSetSystemLabelError> Errors { get; set; } = [];
}

[InterfaceType("BulkSetSystemLabelError")]
public interface IBulkSetSystemLabelError
{
    string Message { get; set; }
}

public sealed class BulkSetSystemLabelNotFound : IBulkSetSystemLabelError
{
    public string Message { get; set; } = null!;
}

public sealed class BulkSetSystemLabelDomainError : IBulkSetSystemLabelError
{
    public string Message { get; set; } = null!;
}

public sealed class BulkSetSystemLabelValidationError : IBulkSetSystemLabelError
{
    public string Message { get; set; } = null!;
}

public sealed class BulkSetSystemLabelConcurrencyError : IBulkSetSystemLabelError
{
    public string Message { get; set; } = null!;
}
