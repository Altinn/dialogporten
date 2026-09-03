using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Http;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using Digdir.Library.Entity.Abstractions;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;

public sealed class DialogGuiAction : IEntity, IAuthorizationContextCarrier
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

    public Uri Url { get; set; } = null!;
    public string? AuthorizationAttribute { get; set; }

    public bool IsDeleteDialogAction { get; set; }

    // === Dependent relationships ===
    public DialogGuiActionPriority.Values PriorityId { get; set; }
    public DialogGuiActionPriority Priority { get; set; } = null!;

    public HttpVerb.Values HttpMethodId { get; set; } = HttpVerb.Values.GET;
    public HttpVerb HttpMethod { get; set; } = null!;

    public Guid DialogId { get; set; }
    public DialogEntity Dialog { get; set; } = null!;

    // === Principal relationships ===
    [AggregateChild]
    public DialogGuiActionTitle? Title { get; set; }

    [AggregateChild]
    public DialogGuiActionPrompt? Prompt { get; set; }

    [AggregateChild]
    public DialogGuiActionAuthorizationContext? AuthorizationContext { get; set; }

    AuthorizationContext? IAuthorizationContextCarrier.AuthorizationContext => AuthorizationContext;
}

public sealed class DialogGuiActionPrompt : LocalizationSet
{
    public Guid GuiActionId { get; set; }
    public DialogGuiAction GuiAction { get; set; } = null!;
}

public sealed class DialogGuiActionTitle : LocalizationSet
{
    public Guid GuiActionId { get; set; }
    public DialogGuiAction GuiAction { get; set; } = null!;
}
