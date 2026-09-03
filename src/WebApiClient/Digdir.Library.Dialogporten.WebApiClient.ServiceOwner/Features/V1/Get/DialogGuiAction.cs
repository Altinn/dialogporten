using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;

public class DialogGuiAction
{
    /// <summary>
    /// The unique identifier for the action in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The action identifier for the action, corresponding to the "action" attributeId used in the XACML service policy.
    /// <br/>
    /// <br/>Empty when the action was supplied with an authorizationContext, in which case the action is found
    /// <br/>in authorizationContext.action.
    /// </summary>
    [JsonPropertyName("action")]
    [Obsolete("Use 'AuthorizationContext.Action' instead.")]
    public string Action { get; set; } = null!;

    /// <summary>
    /// The fully qualified URL of the action, to which the user will be redirected when the action is triggered.
    /// </summary>
    [JsonPropertyName("url")]
    public required Uri Url { get; set; }

    /// <summary>
    /// Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service
    /// <br/>policy, which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    /// <br/>            
    /// <br/>Can also be used to refer to other service policies.
    /// </summary>
    [JsonPropertyName("authorizationAttribute")]
    [Obsolete("Use 'AuthorizationContext' instead.")]
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// Describes the authorization inputs used when evaluating end user access to this action.
    /// <br/>Null when the action uses legacy authorization fields.
    /// </summary>
    [JsonPropertyName("authorizationContext")]
    [Experimental("DPEXP001", UrlFormat = "https://github.com/Altinn/dialogporten/issues/3978")]
    public AuthorizationContext? AuthorizationContext { get; set; }

    /// <summary>
    /// Whether the user, if supplied in the query, is authorized to perform the action.
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool? IsAuthorized { get; set; }

    /// <summary>
    /// Indicates whether the action results in the dialog being deleted. Used by frontends to implement custom UX
    /// <br/>for delete actions.
    /// </summary>
    [JsonPropertyName("isDeleteDialogAction")]
    public bool IsDeleteDialogAction { get; set; }

    /// <summary>
    /// Indicates a priority for the action, making it possible for frontends to adapt GUI elements based on action
    /// <br/>priority.
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogGuiActionPriority>))]
    public DialogGuiActionPriority Priority { get; set; }

    /// <summary>
    /// The HTTP method that the frontend should use when redirecting the user.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    [JsonConverter(typeof(JsonStringEnumConverter<HttpVerb>))]
    public HttpVerb HttpMethod { get; set; }

    /// <summary>
    /// The title of the action, this should be short and in verb form.
    /// </summary>
    [JsonPropertyName("title")]
    public ICollection<Localization> Title { get; set; } = [];

    /// <summary>
    /// If there should be a prompt asking the user for confirmation before the action is executed,
    /// <br/>this field should contain the prompt text.
    /// </summary>
    [JsonPropertyName("prompt")]
    public ICollection<Localization> Prompt { get; set; } = [];
}
