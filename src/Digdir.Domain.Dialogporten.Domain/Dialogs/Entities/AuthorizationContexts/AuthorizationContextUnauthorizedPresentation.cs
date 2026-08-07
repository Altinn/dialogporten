using Digdir.Library.Entity.Abstractions.Features.Lookup;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

public sealed class AuthorizationContextUnauthorizedPresentation : AbstractLookupEntity<AuthorizationContextUnauthorizedPresentation, AuthorizationContextUnauthorizedPresentation.Values>
{
    public AuthorizationContextUnauthorizedPresentation(Values id) : base(id) { }
    public override AuthorizationContextUnauthorizedPresentation MapValue(Values id) => new(id);

    public enum Values
    {
        Disabled = 1,
        Redacted = 2
    }
}
