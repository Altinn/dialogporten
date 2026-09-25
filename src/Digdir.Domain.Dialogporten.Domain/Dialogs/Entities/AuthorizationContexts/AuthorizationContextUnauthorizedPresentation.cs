namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

/// <summary>
/// How an entity carrying an authorization context is presented to end users that are not authorized for it.
/// Deliberately not a lookup entity: AuthorizationContext is one of the most insert-heavy tables at scale,
/// and a foreign key to a two-row lookup table would cost an extra index plus FOR KEY SHARE locks on the
/// parent rows for every insert. The value is stored as a smallint and mapped to this enum in code only.
/// </summary>
public static class AuthorizationContextUnauthorizedPresentation
{
    public enum Values
    {
        Disabled = 1,
        Excluded = 2
    }
}
