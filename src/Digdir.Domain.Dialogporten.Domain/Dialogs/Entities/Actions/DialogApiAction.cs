using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Library.Entity.Abstractions;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;

public sealed class DialogApiAction : IEntity, IAuthorizationContextCarrier
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// The legacy Altinn action this entity is authorized by. An empty string means "no legacy action;
    /// see <see cref="AuthorizationContext"/>", and is stored instead of NULL so the column stays
    /// NOT NULL and code predating authorization contexts can still materialize the row. Read it through
    /// <see cref="EffectiveLegacyAction"/> rather than comparing to the empty string. Converted to real
    /// nullability in v2.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// <see cref="Action"/> with the empty-string sentinel mapped to null, i.e. the legacy action to
    /// authorize by, or null when this entity is governed by an <see cref="AuthorizationContext"/>.
    /// </summary>
    public string? EffectiveLegacyAction => Action.Length == 0 ? null : Action;

    public string? AuthorizationAttribute { get; set; }
    public string? Name { get; set; }

    // === Dependent relationships ===
    public Guid DialogId { get; set; }
    public DialogEntity Dialog { get; set; } = null!;

    // === Principal relationships ===
    [AggregateChild]
    public List<DialogApiActionEndpoint> Endpoints { get; set; } = [];

    [AggregateChild]
    public DialogApiActionAuthorizationContext? AuthorizationContext { get; set; }

    AuthorizationContext? IAuthorizationContextCarrier.AuthorizationContext => AuthorizationContext;
}
