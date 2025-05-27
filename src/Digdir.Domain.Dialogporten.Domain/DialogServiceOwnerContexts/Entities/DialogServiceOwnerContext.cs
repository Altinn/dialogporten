using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Updatable;
using Digdir.Library.Entity.Abstractions.Features.Versionable;

namespace Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;

public sealed class DialogServiceOwnerContext : IVersionableEntity, ICreatableEntity, IUpdateableEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid Revision { get; set; }

    public Guid DialogId { get; set; }
    public DialogEntity Dialog { get; set; } = null!;

    [AggregateChild]
    public List<DialogServiceOwnerLabel> ServiceOwnerLabels { get; set; } = [];
}
