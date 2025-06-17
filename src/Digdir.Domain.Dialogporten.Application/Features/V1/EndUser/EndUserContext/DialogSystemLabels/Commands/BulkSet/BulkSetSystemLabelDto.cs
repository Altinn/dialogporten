using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.DialogSystemLabels.Commands.BulkSet;

public sealed class BulkSetSystemLabelDto
{
    /// <summary>
    /// List of target dialog ids with optional revision ids
    /// </summary>
    public IReadOnlyCollection<DialogRevisionDto> Dialogs { get; init; } = [];

    /// <summary>
    /// List of system labels to set on target dialogs
    /// </summary>
    public IReadOnlyCollection<SystemLabel.Values> SystemLabels { get; init; } = [];
}

public sealed class DialogRevisionDto
{
    /// <summary>
    /// Target dialog id for system labels
    /// </summary>
    public Guid DialogId { get; init; }

    /// <summary>
    /// Optional end user context revision to match against. If supplied and not matching current revision, the entire operation will fail.
    /// </summary>
    public Guid? EndUserContextRevision { get; init; }
}
