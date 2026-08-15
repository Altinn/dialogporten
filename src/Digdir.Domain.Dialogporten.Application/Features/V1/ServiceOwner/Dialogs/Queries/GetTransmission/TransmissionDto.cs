using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.GetTransmission;

public sealed class TransmissionDto
{
    /// <summary>
    /// The unique identifier for the transmission in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// An optional key to ensure idempotency in transmission creation. If provided, it must be unique within the dialog; reusing the same key for the same dialog results in Conflict and no new transmission is created.
    /// </summary>
    public string? IdempotentKey { get; set; }

    /// <summary>
    /// The date and time when the transmission was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The authorization attribute associated with the transmission.
    /// </summary>
    [Obsolete($"Use '{nameof(AuthorizationContext)}' instead.")]
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// Describes the authorization inputs used when evaluating end user access to this transmission.
    /// Null when no authorization context is set, including when the transmission uses the legacy
    /// "authorizationAttribute".
    /// </summary>
    public AuthorizationContextDto? AuthorizationContext { get; set; }

    /// <summary>
    /// The extended type URI for the transmission.
    /// </summary>
    public Uri? ExtendedType { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// </summary>
    public string? ExternalReference { get; set; }

    /// <summary>
    /// The unique identifier for the related transmission, if any.
    /// </summary>
    public Guid? RelatedTransmissionId { get; set; }

    /// <summary>
    /// The type of the transmission.
    /// </summary>
    public DialogTransmissionType.Values Type { get; set; }

    /// <summary>
    /// The sender actor information for the transmission.
    /// </summary>
    public ActorDto Sender { get; set; } = null!;

    /// <summary>
    /// The content of the transmission.
    /// </summary>
    public ContentDto Content { get; set; } = null!;

    /// <summary>
    /// The attachments associated with the transmission.
    /// </summary>
    public List<AttachmentDto> Attachments { get; set; } = [];

    /// <summary>
    /// The navigational actions associated with the transmission.
    /// </summary>
    public List<NavigationalActionDto> NavigationalActions { get; set; } = [];
}


public sealed class ContentDto : ITransmissionContentDto
{
    /// <summary>
    /// The title of the content.
    /// </summary>
    public ContentValueDto Title { get; set; } = null!;

    /// <summary>
    /// The summary of the content.
    /// </summary>
    public ContentValueDto? Summary { get; set; }

    /// <summary>
    /// Front-channel embedded content. Used to dynamically embed content in the frontend from an external URL.
    /// </summary>
    public ContentValueDto? ContentReference { get; set; }
}

public sealed class AttachmentDto
{
    /// <summary>
    /// The unique identifier for the attachment in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The display name of the attachment that should be used in GUIs.
    /// </summary>
    public List<LocalizationDto> DisplayName { get; set; } = [];

    /// <summary>
    /// The logical name of the attachment.
    /// </summary>
    /// <example>receipt</example>
    public string? Name { get; set; }

    /// <summary>
    /// The URLs associated with the attachment, each referring to a different representation of the attachment.
    /// </summary>
    public List<AttachmentUrlDto> Urls { get; set; } = [];

    /// <summary>
    /// The UTC timestamp when the attachment expires and is no longer available.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Describes additional authorization inputs used when evaluating end user access to this attachment.
    /// The XACML action defaults to "read". Access to the parent transmission is always required in addition;
    /// this context can only further restrict access, never widen it.
    /// </summary>
    public AuthorizationContextDto? AuthorizationContext { get; set; }
}

public sealed class AttachmentUrlDto
{
    /// <summary>
    /// The unique identifier for the attachment URL in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The fully qualified URL of the attachment. Will be set to "urn:dialogporten:unauthorized" if the user is
    /// not authorized to access the transmission.
    /// </summary>
    /// <example>
    /// https://someendpoint.com/someattachment.pdf
    /// urn:dialogporten:unauthorized
    /// </example>
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The media type of the attachment.
    /// </summary>
    /// <example>
    /// application/pdf
    /// application/zip
    /// </example>
    public string? MediaType { get; set; }

    /// <summary>
    /// The type of consumer the URL is intended for.
    /// </summary>
    public AttachmentUrlConsumerType.Values ConsumerType { get; set; }
}

public sealed class NavigationalActionDto
{
    /// <summary>
    /// The title of the navigational action.
    /// </summary>
    public List<LocalizationDto> Title { get; set; } = [];

    /// <summary>
    /// The fully qualified URL of the navigational action.
    /// </summary>
    /// <example>
    /// https://example.com/path
    /// </example>
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The UTC timestamp when the navigational action expires and is no longer available.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Describes additional authorization inputs used when evaluating end user access to this navigational action.
    /// The XACML action defaults to "read". Access to the parent transmission is always required in addition;
    /// this context can only further restrict access, never widen it.
    /// </summary>
    public AuthorizationContextDto? AuthorizationContext { get; set; }
}

public sealed class AuthorizationContextDto
{
    /// <summary>
    /// A service resource that overrides the dialog's own service resource in the authorization evaluation,
    /// referring to another service policy.
    /// </summary>
    /// <example>urn:altinn:resource:some-other-service-identifier</example>
    public string? ServiceResource { get; set; }

    /// <summary>
    /// An additional resource attribute to be matched within the effective service policy, e.g. a task or
    /// subresource.
    /// </summary>
    /// <example>
    /// urn:altinn:task:Task_1
    /// urn:altinn:subresource:mycustomresource
    /// </example>
    public string? AdditionalResourceAttribute { get; set; }

    /// <summary>
    /// The parties access is evaluated on behalf of. Access is granted if the end user has access to the
    /// effective resource for at least one of the parties.
    /// </summary>
    /// <example>urn:altinn:organization:identifier-no:912345678</example>
    public List<string> Parties { get; set; } = [];

    /// <summary>
    /// Whether the dialog's own party is included in the evaluation in addition to "parties".
    /// </summary>
    public bool IncludeDialogParty { get; set; }

    /// <summary>
    /// The XACML action to evaluate. Null when not overridden; the effective action is then "read".
    /// </summary>
    /// <example>read</example>
    public string? Action { get; set; }

    /// <summary>
    /// Controls how the entity is presented to end users that fail the authorization check:
    /// "disabled" keeps the entity visible but masks its URLs and embedded content references, while
    /// "redacted" additionally strips all content (titles, summaries, names, senders and children),
    /// leaving only the entity's existence and timestamps.
    /// </summary>
    public AuthorizationContextUnauthorizedPresentation.Values UnauthorizedPresentation { get; set; }
}
