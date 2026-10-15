using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

public class DialogPartyCheckInput
{
    /// <summary>
    /// Required. Restricts how the end user's access at the dialog's party must have been obtained for this
    /// <br/>check to grant access: "none" accepts all access, "directAndHierarchy" only access that does not pass
    /// <br/>through another organization (direct grants, ER roles and their role mappings at the dialog party, and
    /// <br/>inheritance from its main unit), and "clientDelegation" only client delegations made through the
    /// <br/>organization given in "viaParty".
    /// </summary>
    [JsonPropertyName("accessRestriction")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogPartyCheckAccessRestriction>))]
    public DialogPartyCheckAccessRestriction AccessRestriction { get; set; }

    /// <summary>
    /// The organization the end user's access must have been client delegated through. Required when
    /// <br/>"accessRestriction" is "clientDelegation", and not allowed otherwise. Cannot be the dialog's own party.
    /// </summary>
    [JsonPropertyName("viaParty")]
    public string? ViaParty { get; set; }

    /// <summary>
    /// A service resource to evaluate for this check instead of the context's. If omitted, the context's
    /// <br/>"serviceResource" is used, or the dialog's own service resource if the context sets none. The same rules
    /// <br/>apply as for the context's "serviceResource", including that the service owner must have access to it.
    /// <br/>The context's "additionalResourceAttribute", if any, is layered on the resulting resource.
    /// </summary>
    [JsonPropertyName("serviceResource")]
    public string? ServiceResource { get; set; }

    /// <summary>
    /// The XACML action to evaluate for this check instead of the context's. If omitted, the context's "action"
    /// <br/>is used, or "read" if the context sets none.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }
}
