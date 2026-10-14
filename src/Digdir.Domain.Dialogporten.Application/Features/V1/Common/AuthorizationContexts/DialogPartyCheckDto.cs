using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;

[ExperimentalFeature(ExperimentalFeatures.AuthorizationContext)]
public sealed class DialogPartyCheckDto
{
    /// <summary>
    /// Required. Restricts how the end user's access at the dialog's party must have been obtained for this
    /// check to grant access: "none" accepts all access, "directAndHierarchy" only access that does not pass
    /// through another organization (direct grants, ER roles and their role mappings at the dialog party, and
    /// inheritance from its main unit), and "clientDelegation" only client delegations made through the
    /// organization given in "viaParty".
    /// </summary>
    public DialogPartyCheckAccessRestriction.Values AccessRestriction { get; set; }

    /// <summary>
    /// The organization the end user's access must have been client delegated through. Required when
    /// "accessRestriction" is "clientDelegation", and not allowed otherwise. Cannot be the dialog's own party.
    /// </summary>
    /// <example>urn:altinn:organization:identifier-no:912345678</example>
    public string? ViaParty { get; set; }

    /// <summary>
    /// A service resource to evaluate for this check instead of the context's. If omitted, the context's
    /// "serviceResource" is used, or the dialog's own service resource if the context sets none. The same rules
    /// apply as for the context's "serviceResource", including that the service owner must have access to it.
    /// The context's "additionalResourceAttribute", if any, is layered on the resulting resource.
    /// </summary>
    /// <example>urn:altinn:resource:some-other-service-identifier</example>
    public string? ServiceResource { get; set; }

    /// <summary>
    /// The XACML action to evaluate for this check instead of the context's. If omitted, the context's "action"
    /// is used, or "read" if the context sets none.
    /// </summary>
    /// <example>readall</example>
    public string? Action { get; set; }
}
