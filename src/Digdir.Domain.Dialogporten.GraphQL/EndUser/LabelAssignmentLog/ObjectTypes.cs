using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.LabelAssignmentLog;

[InterfaceType("LabelAssignmentLogError")]
public interface ILabelAssignmentLogError
{
    string Message { get; set; }
}

public sealed class LabelAssignmentLogNotFound : ILabelAssignmentLogError
{
    public required string Message { get; set; }
}

public sealed class LabelAssignmentLogDeleted : ILabelAssignmentLogError
{
    public required string Message { get; set; }
}

public sealed class LabelAssignmentLogForbidden : ILabelAssignmentLogError
{
    public string Message { get; set; } = "Forbidden";
}

public sealed class LabelAssignmentLogPayload
{
    [GraphQLDescription("The immutable list of label assignment log entries for the dialog's end-user context.")]
    public List<LabelAssignmentLog> LabelAssignmentLog { get; set; } = [];
    public List<ILabelAssignmentLogError> Errors { get; set; } = [];
}

public sealed class LabelAssignmentLog
{
    [GraphQLDescription("The date and time when the label assignment log entry was created.")]
    public required DateTimeOffset CreatedAt { get; set; }

    [GraphQLDescription("The name of the system label that was changed.")]
    public required string Name { get; set; }

    [GraphQLDescription("The action that was performed on the label, e.g. 'set' or 'removed'.")]
    public required string Action { get; set; }

    [GraphQLDescription("The actor that performed the label assignment.")]
    public required Actor PerformedBy { get; set; }
}
