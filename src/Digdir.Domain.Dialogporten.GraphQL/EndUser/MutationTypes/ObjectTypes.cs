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

    [GraphQLDescription("List of system labels to add to the target dialog. If multiple instances of 'bin', 'archive', or 'default' are provided, the last one will be used.")]
    public List<SystemLabel> AddLabels { get; set; } = [];

    [GraphQLDescription("List of system labels to remove from the target dialog. If 'bin' or 'archive' is removed, the 'default' label will be added automatically unless 'bin' or 'archive' is also in the AddLabels list.")]
    public List<SystemLabel> RemoveLabels { get; set; } = [];
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

public sealed class SetSystemLabelConflictError : ISetSystemLabelError
{
    public string Message { get; set; } = null!;
}

public sealed class BulkSetSystemLabelInput
{
    public List<DialogRevisionInput> Dialogs { get; set; } = [];

    [GraphQLDescription("List of system labels to add to the target dialogs. If multiple instances of 'bin', 'archive', or 'default' are provided, the last one will be used.")]
    public List<SystemLabel> AddLabels { get; set; } = [];

    [GraphQLDescription("List of system labels to remove from the target dialogs. If 'bin' or 'archive' is removed, the 'default' label will be added automatically unless 'bin' or 'archive' is also in the AddLabels list.")]
    public List<SystemLabel> RemoveLabels { get; set; } = [];
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

public sealed class BulkSetSystemLabelConflictError : IBulkSetSystemLabelError
{
    public string Message { get; set; } = null!;
}
