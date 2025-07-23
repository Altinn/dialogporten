using System.ComponentModel.DataAnnotations.Schema;
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

    public List<SystemLabel.Values> SystemLabelIds { get; private set; } = [];
    [NotMapped]
    public List<SystemLabel> SystemLabels { get; private set; } = [];

    [AggregateChild]
    public IReadOnlyCollection<LabelAssignmentLog> LabelAssignmentLogs => _labelAssignmentLogs.AsReadOnly();

    public void UpdateLabel(SystemLabel.Values newLabel, string userId, ActorType.Values actorType = ActorType.Values.PartyRepresentative)
    {
        var currentLabel = SystemLabelIds.FirstOrDefault(l => SystemLabel.MutuallyExclusiveLabels.Contains(l));
        if (newLabel == currentLabel)
        {
            return;
        }

        SystemLabelIds.Remove(currentLabel);
        SystemLabelIds.Add(newLabel);

        // No need to store actor name for ServiceOwner label updates
        var actorNameEntity = actorType == ActorType.Values.PartyRepresentative
            ? new ActorName
            {
                ActorId = userId
            } : null;

        // remove old label then add new one
        if (currentLabel != SystemLabel.Values.Default)
        {
            _labelAssignmentLogs.Add(new()
            {
                Name = currentLabel.ToNamespacedName(),
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
