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

    public string? Action { get; set; }
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
