using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels;

public sealed class BulkSetSystemLabelDto
{
    private readonly List<SystemLabel.Values> _addLabels = [];

    /// <summary>
    /// List of target dialog ids with optional revision ids
    /// </summary>
    public IReadOnlyCollection<DialogRevisionDto> Dialogs { get; init; } = [];

    /// <summary>
    /// List of system labels to set on target dialogs
    /// </summary>
    [Obsolete("Use AddLabels instead. This property will be removed in a future version.")]
    public IReadOnlyCollection<SystemLabel.Values> SystemLabels
    {
        get => _addLabels;
        init => _addLabels.AddRange(value);
    }

    /// <summary>
    /// List of system labels to add to target dialogs. If multiple instances of 'bin', 'archive', or 'default' are provided, the last one will be used.
    /// </summary>
    public IReadOnlyCollection<SystemLabel.Values> AddLabels
    {
        get => _addLabels;
        init => _addLabels.AddRange(value);
    }

    /// <summary>
    /// List of system labels to remove from target dialogs. If 'bin' or 'archive' is removed, the 'default' label will be added automatically unless 'bin' or 'archive' is also in the AddLabels list.
    /// </summary>
    public IReadOnlyCollection<SystemLabel.Values> RemoveLabels { get; init; } = [];
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
