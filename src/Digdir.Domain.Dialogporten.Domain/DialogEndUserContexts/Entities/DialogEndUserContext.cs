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

    public List<DialogEndUserContextSystemLabel> DialogEndUserContextSystemLabels { get; private set; } = [];

    [AggregateChild]
    public IReadOnlyCollection<LabelAssignmentLog> LabelAssignmentLogs => _labelAssignmentLogs.AsReadOnly();

    public void UpdateSystemLabels(
        IEnumerable<SystemLabel.Values> addLabels,
        IEnumerable<SystemLabel.Values> removeLabels,
        string userId,
        ActorType.Values actorType = ActorType.Values.PartyRepresentative)
    {
        var performedBy = CreateLabelAssignmentLogActor(userId, actorType);

        var current = DialogEndUserContextSystemLabels
            .Select(x => x.SystemLabelId)
            .ToList();

        var next = current
            .ToList()
            .RemoveSystemLabels(removeLabels)
            .AddSystemLabels(addLabels);

        SetSystemLabelEntities(next, current, performedBy);
    }

    private void SetSystemLabelEntities(List<SystemLabel.Values> next, List<SystemLabel.Values> current, LabelAssignmentLogActor performedBy)
    {
        foreach (var addedValue in next.Distinct().Except(current))
        {
            AddSystemLabelEntity(addedValue, performedBy);
        }

        foreach (var removedValue in current.Except(next))
        {
            RemoveSystemLabelEntity(removedValue, performedBy);
        }
    }

    private void RemoveSystemLabelEntity(SystemLabel.Values removedValue, LabelAssignmentLogActor performedBy)
    {
        DialogEndUserContextSystemLabels.RemoveAll(x => x.SystemLabelId == removedValue);

        if (removedValue != SystemLabel.Values.Default)
        {
            LogLabelAssignment(performedBy, removedValue, "remove");
        }
    }

    private void AddSystemLabelEntity(SystemLabel.Values addedValue, LabelAssignmentLogActor performedBy)
    {
        DialogEndUserContextSystemLabels.Add(new() { SystemLabelId = addedValue });

        if (addedValue != SystemLabel.Values.Default)
        {
            LogLabelAssignment(performedBy, addedValue, "set");
        }
    }

    private void LogLabelAssignment(LabelAssignmentLogActor performedBy, SystemLabel.Values labelValue,
        string action) =>
        _labelAssignmentLogs.Add(new LabelAssignmentLog
        {
            Name = labelValue.ToNamespacedName(),
            Action = action,
            PerformedBy = performedBy
        });

    private static LabelAssignmentLogActor CreateLabelAssignmentLogActor(string userId, ActorType.Values actorType) =>
        new()
        {
            ActorTypeId = actorType,
            ActorNameEntity = actorType != ActorType.Values.PartyRepresentative
                ? null
                : new ActorName
                {
                    ActorId = userId
                }
        };
}
