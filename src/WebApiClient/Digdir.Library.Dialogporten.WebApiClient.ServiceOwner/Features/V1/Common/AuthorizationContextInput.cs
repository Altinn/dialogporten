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
    /// Other parties than the dialog's to evaluate access on behalf of, each at its own party and without access
    /// <br/>restriction. Access is granted if the end user has access to the effective resource for at least one of
    /// <br/>the parties, or if any of "dialogPartyChecks" permits. Cannot contain the dialog's own party; use a
    /// <br/>"dialogPartyChecks" entry with access restriction "None" for that. Together with "dialogPartyChecks", at
    /// <br/>least 1 and at most 4 entries.
    /// </summary>
    [JsonPropertyName("parties")]
    public ICollection<string> Parties { get; set; } = [];

    /// <summary>
    /// Checks evaluated on behalf of the dialog's own party. Access is granted if any check, or any entry in
    /// <br/>"parties", permits. An empty list means the dialog's party is not evaluated. Together with "parties",
    /// <br/>at least 1 and at most 4 entries.
    /// </summary>
    [JsonPropertyName("dialogPartyChecks")]
    public ICollection<DialogPartyCheckInput> DialogPartyChecks { get; set; } = [];

    /// <summary>
    /// The XACML action to evaluate. Optional; defaults to "read" if not supplied.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// An optional reference identifying this context in the dialog token. When the end user is authorized for
    /// <br/>this context, the dialog token's "e" claim lists this value instead of the id of the entity carrying the
    /// <br/>context, allowing the service owner to recognize the grant without tracking Dialogporten entity ids.
    /// <br/>Sharing a value between entities in one dialog forms an OR-group: authorization for any group member
    /// <br/>adds the shared value to "e", so only group entities that intentionally share access semantics.
    /// <br/>Recipients must validate the token's dialog id ("i") together with the reference.
    /// <br/>Maximum 50 characters.
    /// </summary>
    [JsonPropertyName("tokenRef")]
    public string? TokenRef { get; set; }

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
