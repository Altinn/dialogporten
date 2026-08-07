using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Library.Entity.Abstractions;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;

public sealed class DialogApiAction : IEntity, IAuthorizationContextCarrier
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public string? Action { get; set; }
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
