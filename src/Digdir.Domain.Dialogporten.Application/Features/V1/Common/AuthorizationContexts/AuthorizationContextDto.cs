using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;

public sealed class AuthorizationContextDto : IAuthorizationContextDtoBase
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
    public List<string> Parties { get; set; } = [];

    /// <summary>
    /// Whether the dialog's own party is included in the evaluation in addition to "parties".
    /// </summary>
    public bool IncludeDialogParty { get; set; }

    /// <summary>
    /// The XACML action to evaluate. Required on API and GUI actions. Optional on transmissions; if not
    /// supplied, "read" is used when "serviceResource" is set, otherwise "transmissionread".
    /// </summary>
    /// <example>read</example>
    public string? Action { get; set; }

    /// <summary>
    /// Required. Controls how the entity is presented to end users that fail the authorization check:
    /// "disabled" keeps the entity visible but masks its URLs and embedded content references, while
    /// "redacted" additionally strips all content (titles, summaries, names, senders and children),
    /// leaving only the entity's existence and timestamps.
    /// </summary>
    public AuthorizationContextUnauthorizedPresentation.Values UnauthorizedPresentation { get; set; }
}

public sealed class ChildAuthorizationContextDto : IAuthorizationContextDtoBase
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
    public List<string> Parties { get; set; } = [];

    /// <summary>
    /// Whether the dialog's own party is included in the evaluation in addition to "parties".
    /// </summary>
    public bool IncludeDialogParty { get; set; }

    /// <summary>
    /// Required. Controls how the entity is presented to end users that fail the authorization check:
    /// "disabled" keeps the entity visible but masks its URLs and embedded content references, while
    /// "redacted" additionally strips all content (titles, summaries, names, senders and children),
    /// leaving only the entity's existence and timestamps.
    /// </summary>
    public AuthorizationContextUnauthorizedPresentation.Values UnauthorizedPresentation { get; set; }
}

internal interface IAuthorizationContextDtoBase
{
    string? ServiceResource { get; }
    string? AdditionalResourceAttribute { get; }
    List<string> Parties { get; }
    bool IncludeDialogParty { get; }
    AuthorizationContextUnauthorizedPresentation.Values UnauthorizedPresentation { get; }
}
