namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

/// <summary>
/// Restricts how an end user's access at the dialog's party must have been obtained for a dialog party check to
/// grant access. The values match the PDP's access restriction values one to one. Deliberately not a lookup
/// entity, for the same reason as <see cref="AuthorizationContextUnauthorizedPresentation"/>; the value is stored
/// inside the context's dialog party checks JSON column.
/// </summary>
public static class DialogPartyCheckAccessRestriction
{
    public enum Values
    {
        /// <summary>All access counts, however it was obtained.</summary>
        None = 1,

        /// <summary>
        /// Only access that reaches the end user without passing through another organization: direct grants,
        /// ER roles (and their role mappings) at the dialog party, and inheritance from its main unit.
        /// </summary>
        DirectAndHierarchy = 2,

        /// <summary>Only client delegations made through the organization given as via-party.</summary>
        ClientDelegation = 3
    }
}
