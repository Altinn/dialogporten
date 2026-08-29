using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;

public sealed class AuthorizationContextDto
{
    /// <summary>
    /// A service resource that overrides the dialog's own service resource in the authorization evaluation,
    /// referring to another service policy. The service owner must have access to the referenced resource.
    /// When set, the dialog's instance reference no longer applies to the evaluation of this entity.
    /// </summary>
    /// <example>urn:altinn:resource:some-other-service-identifier</example>
    public string? ServiceResource { get; set; }

    /// <summary>
    /// An additional resource attribute to be matched within the effective service policy, e.g. a task or
    /// subresource. Cannot contain a service resource reference; use "serviceResource" for that.
    /// </summary>
    /// <example>
    /// urn:altinn:task:Task_1
    /// urn:altinn:subresource:mycustomresource
    /// </example>
    public string? AdditionalResourceAttribute { get; set; }

    /// <summary>
    /// The parties to evaluate access on behalf of. Access is granted if the end user has access to the
    /// effective resource for at least one of the parties. Must contain at least one party unless
    /// "includeDialogParty" is true.
    /// </summary>
    /// <example>urn:altinn:organization:identifier-no:912345678</example>
    public List<string> Parties
    {
        get;
        // Nullable in the OpenAPI schema (and reachable as an explicit JSON null via the JsonPatch-based
        // update endpoint, which binds through Newtonsoft rather than the STJ pipeline's null-annotation
        // enforcement); normalize here so every consumer - validator, mapper - can assume a non-null list.
        set => field = value ?? [];
    } = [];

    /// <summary>
    /// Whether the dialog's own party is included in the evaluation in addition to "parties".
    /// </summary>
    public bool IncludeDialogParty { get; set; }

    /// <summary>
    /// The XACML action to evaluate. Optional; defaults to "read" if not supplied.
    /// </summary>
    /// <example>read</example>
    public string? Action { get; set; }

    /// <summary>
    /// Required. Controls how the entity is presented to end users that fail the authorization check:
    /// "disabled" keeps the entity visible but masks its URLs and embedded content references, while
    /// "excluded" removes it from the collection it belongs to entirely, leaving only its id and
    /// creation time in the sibling "excluded" list (e.g. "excludedTransmissions" beside
    /// "transmissions").
    /// </summary>
    public AuthorizationContextUnauthorizedPresentation.Values UnauthorizedPresentation { get; set; }
}
