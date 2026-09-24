using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;

public class AuthorizationContextDetails
{
    /// <summary>
    /// A service resource that overrides the dialog's own service resource in the authorization evaluation,
    /// <br/>referring to another service policy.
    /// </summary>
    [JsonPropertyName("serviceResource")]
    public string? ServiceResource { get; set; }

    /// <summary>
    /// An additional resource attribute to be matched within the effective service policy, e.g. a task or
    /// <br/>subresource.
    /// </summary>
    [JsonPropertyName("additionalResourceAttribute")]
    public string? AdditionalResourceAttribute { get; set; }

    /// <summary>
    /// The parties access is evaluated on behalf of. Access is granted if the end user has access to the
    /// <br/>effective resource for at least one of the parties.
    /// </summary>
    [JsonPropertyName("parties")]
    public ICollection<string> Parties { get; set; } = [];

    /// <summary>
    /// Whether the dialog's own party is included in the evaluation in addition to "parties".
    /// </summary>
    [JsonPropertyName("includeDialogParty")]
    public bool IncludeDialogParty { get; set; }

    /// <summary>
    /// The XACML action to evaluate. Null when not overridden; the effective action is then "read".
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// The service owner supplied reference identifying this context in the dialog token's "e" claim, if any.
    /// <br/>Null when the carrying entity's id is used instead. Sharing a value between entities in one dialog
    /// <br/>forms an OR-group: authorization for any group member adds the shared value to "e". Recipients must
    /// <br/>also validate the token's dialog id ("i").
    /// </summary>
    [JsonPropertyName("tokenRef")]
    public string? TokenRef { get; set; }

    /// <summary>
    /// Controls how the entity is presented to end users that fail the authorization check:
    /// <br/>"disabled" keeps the entity visible but masks its URLs and embedded content references, while
    /// <br/>"excluded" removes it from the collection it belongs to entirely, leaving only its id and
    /// <br/>creation time in the sibling "excluded" list (e.g. "excludedTransmissions" beside "transmissions").
    /// </summary>
    [JsonPropertyName("unauthorizedPresentation")]
    [JsonConverter(typeof(JsonStringEnumConverter<AuthorizationContextUnauthorizedPresentation>))]
    public AuthorizationContextUnauthorizedPresentation UnauthorizedPresentation { get; set; }
}
