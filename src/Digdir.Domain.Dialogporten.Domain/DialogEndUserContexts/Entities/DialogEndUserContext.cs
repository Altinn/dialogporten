using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Library.Entity.Abstractions;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Versionable;

namespace Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

public sealed class DialogEndUserContext : IEntity, IVersionableEntity
{
    private readonly List<LabelAssignmentLog> _labelAssignmentLogs = [];

    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid Revision { get; set; }

    public Guid? DialogId { get; set; }
    public DialogEntity? Dialog { get; set; }

    public List<DialogEndUserContextSystemLabel> DialogEndUserContextSystemLabels { get; private set; } = [];

    [AggregateChild]
    public IReadOnlyCollection<LabelAssignmentLog> LabelAssignmentLogs => _labelAssignmentLogs.AsReadOnly();

    public void UpdateRequiredMutuallyExclusiveLabel(SystemLabel.Values newLabel, string userId, ActorType.Values actorType = ActorType.Values.PartyRepresentative)
    {
        var currentLabel = DialogEndUserContextSystemLabels.First(l =>
            SystemLabel.MutuallyExclusiveRequiredLabels.Contains(l.SystemLabelId));

        if (newLabel == currentLabel.SystemLabelId)
        {
            return;
        }

        DialogEndUserContextSystemLabels.Remove(currentLabel);
        DialogEndUserContextSystemLabels.Add(new() { SystemLabelId = newLabel });

        // No need to store actor name for ServiceOwner label updates
        var actorNameEntity = actorType == ActorType.Values.PartyRepresentative
            ? new ActorName
            {
                ActorId = userId
            } : null;

        // remove old label then add new one
        if (currentLabel.SystemLabelId != SystemLabel.Values.Default)
        {
            _labelAssignmentLogs.Add(new()
            {
                Name = currentLabel.SystemLabelId.ToNamespacedName(),
                Action = "remove",
                PerformedBy = new()
                {
                    ActorTypeId = actorType,
                    ActorNameEntity = actorNameEntity
                }
            });
        }

        if (newLabel != SystemLabel.Values.Default)
        {
            _labelAssignmentLogs.Add(new()
            {
                Name = newLabel.ToNamespacedName(),
                Action = "set",
                PerformedBy = new()
                {
                    ActorTypeId = actorType,
                    ActorNameEntity = actorNameEntity
                }
            });
        }
    }
}

public sealed class DialogEndUserContextSystemLabel : ICreatableEntity
{
    public SystemLabel.Values SystemLabelId { get; internal set; } = SystemLabel.Values.Default;
    public SystemLabel SystemLabel { get; private set; } = null!;

    public Guid DialogEndUserContextId { get; set; }
    public DialogEndUserContext DialogEndUserContext { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
