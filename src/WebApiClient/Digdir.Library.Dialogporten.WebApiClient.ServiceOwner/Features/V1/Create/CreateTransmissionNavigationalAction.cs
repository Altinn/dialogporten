using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;

public class CreateTransmissionNavigationalAction
{
    /// <summary>
    /// The title of the navigational action.
    /// </summary>
    [JsonPropertyName("title")]
    public ICollection<Localization> Title { get; set; } = [];

    /// <summary>
    /// The fully qualified URL of the navigational action.
    /// </summary>
    [JsonPropertyName("url")]
    public required Uri Url { get; set; }

    /// <summary>
    /// The UTC timestamp when the navigational action expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Describes additional authorization inputs used when evaluating end user access to this navigational action.
    /// <br/>The XACML action defaults to "read". Access to the parent transmission is always required in addition; this
    /// <br/>context can only further restrict access, never widen it.
    /// </summary>
    [JsonPropertyName("authorizationContext")]
    [Experimental("DPEXP001", UrlFormat = "https://github.com/Altinn/dialogporten/issues/3978")]
    public AuthorizationContextInput? AuthorizationContext { get; set; }
}
