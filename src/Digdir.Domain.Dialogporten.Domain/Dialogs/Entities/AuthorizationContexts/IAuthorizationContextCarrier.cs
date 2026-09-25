namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

/// <summary>
/// Implemented by entities that may carry an <see cref="AuthorizationContexts.AuthorizationContext"/>.
/// </summary>
public interface IAuthorizationContextCarrier
{
    AuthorizationContext? AuthorizationContext { get; }
}
