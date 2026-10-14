using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;

public class DialogPartyCheckDetails
{
    /// <summary>
    /// Restricts how the end user's access at the dialog's party must have been obtained for this check to grant
    /// <br/>access: "none" accepts all access, "directAndHierarchy" only access that does not pass through another
    /// <br/>organization, and "clientDelegation" only client delegations made through the organization in "viaParty".
    /// </summary>
    [JsonPropertyName("accessRestriction")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogPartyCheckAccessRestriction>))]
    public DialogPartyCheckAccessRestriction AccessRestriction { get; set; }

    /// <summary>
    /// The organization the end user's access must have been client delegated through. Only set when
    /// <br/>"accessRestriction" is "clientDelegation".
    /// </summary>
    [JsonPropertyName("viaParty")]
    public string? ViaParty { get; set; }

    /// <summary>
    /// The service resource evaluated for this check instead of the context's. Null when not overridden.
    /// </summary>
    [JsonPropertyName("serviceResource")]
    public string? ServiceResource { get; set; }

    /// <summary>
    /// The XACML action evaluated for this check instead of the context's. Null when not overridden.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }
}
