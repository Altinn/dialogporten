using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;

public class DialogAttachment
{
    /// <summary>
    /// The unique identifier for the attachment in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The display name of the attachment that should be used in GUIs.
    /// </summary>
    [JsonPropertyName("displayName")]
    public ICollection<Localization> DisplayName { get; set; } = [];

    /// <summary>
    /// The logical name of the attachment.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The URLs associated with the attachment, each referring to a different representation of the attachment.
    /// </summary>
    [JsonPropertyName("urls")]
    public ICollection<DialogAttachmentUrl> Urls { get; set; } = [];

    /// <summary>
    /// The UTC timestamp when the attachment expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Describes additional authorization inputs used when evaluating end user access to this attachment.
    /// <br/>The XACML action defaults to "read". Access to the parent is always required in addition; this context
    /// <br/>can only further restrict access, never widen it.
    /// </summary>
    [JsonPropertyName("authorizationContext")]
    public AuthorizationContext? AuthorizationContext { get; set; }

    /// <summary>
    /// Indicates whether the end user is authorized for this attachment.
    /// <br/>
    /// <br/>IsAuthorized is evaluated only when you use the EndUserId query-parameter, otherwise it is null.
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool? IsAuthorized { get; set; }
}
