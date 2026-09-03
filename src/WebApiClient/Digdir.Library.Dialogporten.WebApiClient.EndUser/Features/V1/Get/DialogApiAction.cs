using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Get;

public class DialogApiAction
{
    /// <summary>
    /// The unique identifier for the action in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// String identifier for the action, corresponding to the "action" attributeId used in the XACML service policy,
    /// <br/>which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    /// </summary>
    [JsonPropertyName("action")]
    public required string Action { get; set; }

    /// <summary>
    /// Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service
    /// <br/>policy, which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    /// <br/>
    /// <br/>Can also be used to refer to other service policies.
    /// </summary>
    [JsonPropertyName("authorizationAttribute")]
    [Obsolete("Use of 'authorizationContext' on the service owner API is preferred; this field only reflects the legacy authorization attribute.")]
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// True if the authenticated user is authorized for this action. If not, the action will not be available
    /// <br/>and all endpoints will be replaced with a fixed placeholder.
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// A token asserting the authenticated user's authorization for this specific action, as determined by its
    /// <br/>authorization context. Only present when the action has an authorization context and the user is authorized.
    /// <br/>Should be used instead of the dialog token against this action's endpoints.
    /// </summary>
    [JsonPropertyName("contextToken")]
    [Experimental("DPEXP001", UrlFormat = "https://github.com/Altinn/dialogporten/issues/3978")]
    public string? ContextToken { get; set; }

    /// <summary>
    /// The logical name of the operation the API action refers to.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The endpoints associated with the action.
    /// </summary>
    [JsonPropertyName("endpoints")]
    public ICollection<DialogApiActionEndpoint> Endpoints { get; set; } = [];
}
