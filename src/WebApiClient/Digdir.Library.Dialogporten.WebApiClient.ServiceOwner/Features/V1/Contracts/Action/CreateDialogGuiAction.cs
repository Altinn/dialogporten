using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Action;

public class CreateDialogGuiAction
{
    /// <summary>
    /// A self-defined UUIDv7 may be provided to support idempotent creation of Gui Actions. If not provided, a new UUIDv7 will be generated.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// The action identifier for the action, corresponding to the "action" attributeId used in the XACML service policy.
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = null!;

    /// <summary>
    /// The fully qualified URL of the action, to which the user will be redirected when the action is triggered. Will be set to
    /// <br/>"urn:dialogporten:unauthorized" if the user is not authorized to perform the action.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service
    /// <br/>policy, which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    /// <br/>
    /// <br/>Can also be used to refer to other service policies.
    /// </summary>
    [JsonPropertyName("authorizationAttribute")]
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// Indicates whether the action results in the dialog being deleted. Used by frontends to implement custom UX
    /// <br/>for delete actions.
    /// </summary>
    [JsonPropertyName("isDeleteDialogAction")]
    public bool IsDeleteDialogAction { get; set; }

    /// <summary>
    /// The HTTP method that the frontend should use when redirecting the user.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    [JsonConverter(typeof(JsonStringEnumConverter<HttpVerb>))]
    public HttpVerb? HttpMethod { get; set; }

    /// <summary>
    /// Indicates a priority for the action, making it possible for frontends to adapt GUI elements based on action
    /// <br/>priority.
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogGuiActionPriority>))]
    public DialogGuiActionPriority Priority { get; set; }

    /// <summary>
    /// The title of the action, this should be short and in verb form. Must be text/plain.
    /// </summary>
    [JsonPropertyName("title")]
    public ICollection<Localization>? Title { get; set; }

    /// <summary>
    /// If there should be a prompt asking the user for confirmation before the action is executed,
    /// <br/>this field should contain the prompt text.
    /// </summary>
    [JsonPropertyName("prompt")]
    public ICollection<Localization>? Prompt { get; set; }
}