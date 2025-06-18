using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchTransmissions;

public sealed class TransmissionDto
{
    /// <summary>
    /// The unique identifier for the transmission in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The date and time when the transmission was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The authorization attribute associated with the transmission.
    /// </summary>
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// Flag indicating if the authenticated user is authorized for this transmission. If not, embedded content and
    /// the attachments will not be available.
    /// </summary>
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// The extended type URI for the transmission.
    /// </summary>
    public Uri? ExtendedType { get; set; }

    /// <summary>
    /// The unique identifier for the related transmission, if any.
    /// </summary>
    public Guid? RelatedTransmissionId { get; set; }

    /// <summary>
    /// The date and time when the transmission was deleted, if applicable.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

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
}

public sealed class ContentDto
{
    /// <summary>
    /// The title of the content.
    /// </summary>
    public ContentValueDto Title { get; set; } = null!;

    /// <summary>
    /// The summary of the content.
    /// </summary>
    public ContentValueDto Summary { get; set; } = null!;

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
    /// The URLs associated with the attachment, each referring to a different representation of the attachment.
    /// </summary>
    public List<AttachmentUrlDto> Urls { get; set; } = [];
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
    public string? MediaType { get; set; } = null!;

    /// <summary>
    /// The type of consumer the URL is intended for.
    /// </summary>
    public AttachmentUrlConsumerType.Values ConsumerType { get; set; }
}
