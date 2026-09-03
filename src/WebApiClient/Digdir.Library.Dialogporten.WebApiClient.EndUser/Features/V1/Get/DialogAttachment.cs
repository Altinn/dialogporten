using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Get;

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
    /// Indicates whether the authenticated user is authorized for this attachment. If not, the URLs will be
    /// <br/>replaced with "urn:dialogporten:unauthorized".
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    [Experimental("DPEXP001", UrlFormat = "https://github.com/Altinn/dialogporten/issues/3978")]
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// A token asserting the authenticated user's authorization for this specific attachment, as determined by its
    /// <br/>authorization context. Only present when the attachment has an authorization context and the user is
    /// <br/>authorized. Should be used instead of the dialog token against this attachment's URLs.
    /// </summary>
    [JsonPropertyName("contextToken")]
    [Experimental("DPEXP001", UrlFormat = "https://github.com/Altinn/dialogporten/issues/3978")]
    public string? ContextToken { get; set; }
}
