using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Library.Entity.Abstractions;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;
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

    public List<SystemLabel.Values> SystemLabelIds { get; private set; } = [SystemLabel.Values.Default];
    public List<SystemLabel> SystemLabels { get; private set; } = [];

    [AggregateChild]
    public IReadOnlyCollection<LabelAssignmentLog> LabelAssignmentLogs => _labelAssignmentLogs.AsReadOnly();

    public void UpdateLabel(SystemLabel.Values newLabel, string userId, ActorType.Values actorType = ActorType.Values.PartyRepresentative)
    {
        var currentLabels = SystemLabelIds;
        if (currentLabels.Contains(newLabel))
        {
            return;
        }

        // No need to store actor name for ServiceOwner label updates
        var actorNameEntity = actorType == ActorType.Values.PartyRepresentative
            ? new ActorName
            {
                ActorId = userId
            } : null;

        var actor = new LabelAssignmentLogActor()
        {
            ActorTypeId = actorType,
            ActorNameEntity = actorNameEntity
        };

        if (newLabel.IsExclusive() && currentLabels.Any(x => x.IsExclusive()))
        {
            var label = currentLabels.First(x => x.IsExclusive());
            // remove old label then add new one
            RemoveLabel(label, actor);
        }

        AddLabel(newLabel, actor);
    }


    public void MarkAsUnopened(string userId, ActorType.Values actorType = ActorType.Values.PartyRepresentative)
    {
        if (SystemLabelIds.Contains(SystemLabel.Values.MarkedAsUnopened))
        {
            return;
        }

        var actorNameEntity = actorType == ActorType.Values.PartyRepresentative
            ? new ActorName
            {
                ActorId = userId
            } : null;

        var actor = new LabelAssignmentLogActor()
        {
            ActorTypeId = actorType,
            ActorNameEntity = actorNameEntity
        };

        AddLabel(SystemLabel.Values.MarkedAsUnopened, actor);
    }

    public void UnmarkAsUnopened(string userId, ActorType.Values actorType = ActorType.Values.PartyRepresentative)
    {
        if (!SystemLabelIds.Contains(SystemLabel.Values.MarkedAsUnopened))
        {
            return;
        }

        var actorNameEntity = actorType == ActorType.Values.PartyRepresentative
            ? new ActorName
            {
                ActorId = userId
            } : null;

        var actor = new LabelAssignmentLogActor()
        {
            ActorTypeId = actorType,
            ActorNameEntity = actorNameEntity
        };

        RemoveLabel(SystemLabel.Values.MarkedAsUnopened, actor);
    }

    private void RemoveLabel(SystemLabel.Values label, LabelAssignmentLogActor actor)
    {
        if (label != SystemLabel.Values.Default)
        {
            _labelAssignmentLogs.Add(new()
            {
                Name = label.ToNamespacedName(),
                Action = "remove",
                PerformedBy = actor
            });
        }
        SystemLabelIds.Remove(label);
    }

    private void AddLabel(SystemLabel.Values label, LabelAssignmentLogActor actor)
    {
        if (label != SystemLabel.Values.Default)
        {
            _labelAssignmentLogs.Add(new()
            {
                Name = label.ToNamespacedName(),
                Action = "set",
                PerformedBy = actor
            });
        }
        SystemLabelIds.Add(label);
    }
}
