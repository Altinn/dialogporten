namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

/// <summary>
/// A check evaluated on behalf of the dialog's own party as part of an <see cref="AuthorizationContext"/>.
/// Stored as an element of a JSON column on the context.
/// </summary>
public sealed class DialogPartyCheck
{
    public DialogPartyCheckAccessRestriction.Values AccessRestriction { get; set; }

    /// <summary>
    /// The organization the end user's access must have been client delegated through, e.g.
    /// "urn:altinn:organization:identifier-no:912345678". Only set for
    /// <see cref="DialogPartyCheckAccessRestriction.Values.ClientDelegation"/>.
    /// </summary>
    public string? ViaParty { get; set; }

    /// <summary>
    /// A service resource evaluated instead of the context's. Null falls back to the context's service resource,
    /// then to the dialog's.
    /// </summary>
    public string? ServiceResource { get; set; }

    /// <summary>
    /// The XACML action evaluated instead of the context's. Null falls back to the context's action, then to "read".
    /// </summary>
    public string? Action { get; set; }
}
