using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Get;

public class DialogTransmissionDetails
{
    /// <summary>
    /// The unique identifier for the transmission in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The date and time when the transmission was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The authorization attribute associated with the transmission.
    /// </summary>
    [JsonPropertyName("authorizationAttribute")]
    [Obsolete("Use of 'authorizationContext' on the service owner API is preferred; this field only reflects the legacy authorization attribute.")]
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// Flag indicating if the authenticated user is authorized for this transmission. If not, embedded content and
    /// <br/>the attachments will not be available.
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool IsAuthorized { get; set; }


    /// <summary>
    /// The extended type URI for the transmission.
    /// </summary>
    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// </summary>
    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; }

    /// <summary>
    /// The unique identifier for the related transmission, if any.
    /// </summary>
    [JsonPropertyName("relatedTransmissionId")]
    public Guid? RelatedTransmissionId { get; set; }

    /// <summary>
    /// The date and time when the transmission was deleted, if applicable.
    /// </summary>
    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// The type of the transmission.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogTransmissionType>))]
    public DialogTransmissionType Type { get; set; }

    /// <summary>
    /// The sender actor information for the transmission.
    /// </summary>
    [JsonPropertyName("sender")]
    public required Actor Sender { get; set; }

    /// <summary>
    /// The content of the transmission.
    /// </summary>
    [JsonPropertyName("content")]
    public required DialogTransmissionContentDetails Content { get; set; }

    /// <summary>
    /// The attachments associated with the transmission.
    /// </summary>
    [JsonPropertyName("attachments")]
    public ICollection<DialogTransmissionAttachmentDetails> Attachments { get; set; } = [];

    /// <summary>
    /// Attachments on this transmission that exist but are withheld from the authenticated user, listed by
    /// <br/>id and creation time only. An attachment is excluded rather than shown with masked URLs when its
    /// <br/>authorization context sets unauthorizedPresentation to "excluded".
    /// <br/>
    /// <br/>Exclusions are reported per collection: the full set for a dialog is "excludedAttachments",
    /// <br/>"excludedTransmissions", "excludedGuiActions" and "excludedApiActions" on the dialog, plus
    /// <br/>"excludedAttachments" and "excludedNavigationalActions" on each transmission.
    /// </summary>
    [JsonPropertyName("excludedAttachments")]
    [Experimental("DPEXP001", UrlFormat = "https://github.com/Altinn/dialogporten/issues/3978")]
    public ICollection<ExcludedElement> ExcludedAttachments { get; set; } = [];

    /// <summary>
    /// The navigational actions associated with the transmission.
    /// </summary>
    [JsonPropertyName("navigationalActions")]
    public ICollection<DialogTransmissionNavigationalActionDetails> NavigationalActions { get; set; } = [];

    /// <summary>
    /// Navigational actions on this transmission that exist but are withheld from the authenticated user,
    /// <br/>listed by id and creation time only. A navigational action is excluded rather than returned with
    /// <br/>isAuthorized=false when its authorization context sets unauthorizedPresentation to "excluded".
    /// <br/>
    /// <br/>Exclusions are reported per collection: the full set for a dialog is "excludedAttachments",
    /// <br/>"excludedTransmissions", "excludedGuiActions" and "excludedApiActions" on the dialog, plus
    /// <br/>"excludedAttachments" and "excludedNavigationalActions" on each transmission.
    /// </summary>
    [JsonPropertyName("excludedNavigationalActions")]
    [Experimental("DPEXP001", UrlFormat = "https://github.com/Altinn/dialogporten/issues/3978")]
    public ICollection<ExcludedElement> ExcludedNavigationalActions { get; set; } = [];
}
