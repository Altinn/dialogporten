using Digdir.Library.Entity.Abstractions.Features.Creatable;

namespace Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

public sealed class DialogEndUserContextSystemLabel : ICreatableEntity
{
    public SystemLabel.Values SystemLabelId { get; internal set; } = SystemLabel.Values.Default;
    public SystemLabel SystemLabel { get; private set; } = null!;

    public Guid DialogEndUserContextId { get; set; }
    public DialogEndUserContext DialogEndUserContext { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
