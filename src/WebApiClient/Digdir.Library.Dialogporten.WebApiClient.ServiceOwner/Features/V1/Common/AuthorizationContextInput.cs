using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

public class AuthorizationContextInput
{
    /// <summary>
    /// A service resource that overrides the dialog's own service resource in the authorization evaluation,
    /// <br/>referring to another service policy. The service owner must have access to the referenced resource.
    /// <br/>When set, the dialog's instance reference no longer applies to the evaluation of this entity.
    /// </summary>
    [JsonPropertyName("serviceResource")]
    public string? ServiceResource { get; set; }

    /// <summary>
    /// An additional resource attribute to be matched within the effective service policy, e.g. a task or
    /// <br/>subresource. Cannot contain a service resource reference; use "serviceResource" for that.
    /// <br/>References to an app ("urn:altinn:app") or an organization ("urn:altinn:org") are not allowed
    /// <br/>either; both are derived from the effective service resource.
    /// </summary>
    [JsonPropertyName("additionalResourceAttribute")]
    public string? AdditionalResourceAttribute { get; set; }

    /// <summary>
    /// The parties to evaluate access on behalf of. Access is granted if the end user has access to the
    /// <br/>effective resource for at least one of the parties. Must contain at least one party unless
    /// <br/>"includeDialogParty" is true.
    /// </summary>
    [JsonPropertyName("parties")]
    public ICollection<string> Parties { get; set; } = [];

    /// <summary>
    /// Whether the dialog's own party is included in the evaluation in addition to "parties".
    /// </summary>
    [JsonPropertyName("includeDialogParty")]
    public bool IncludeDialogParty { get; set; }

    /// <summary>
    /// The XACML action to evaluate. Optional; defaults to "read" if not supplied.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// Required. Controls how the entity is presented to end users that fail the authorization check:
    /// <br/>"disabled" keeps the entity visible but masks its URLs and embedded content references, while
    /// <br/>"excluded" removes it from the collection it belongs to entirely, leaving only its id and
    /// <br/>creation time in the sibling "excluded" list (e.g. "excludedTransmissions" beside "transmissions").
    /// </summary>
    [JsonPropertyName("unauthorizedPresentation")]
    [JsonConverter(typeof(JsonStringEnumConverter<AuthorizationContextUnauthorizedPresentation>))]
    public AuthorizationContextUnauthorizedPresentation UnauthorizedPresentation { get; set; }
}
