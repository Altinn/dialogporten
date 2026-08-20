using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Update;

public class UpdateDialogApiAction
{
    /// <summary>
    /// A self-defined UUIDv7 may be provided to support idempotent additions of Api Actions. If not provided, a new UUIDv7 will be generated.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

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
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// The logical name of the operation the API action refers to.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The endpoints associated with the action.
    /// </summary>
    [JsonPropertyName("endpoints")]
    public ICollection<UpdateDialogApiActionEndpoint>? Endpoints { get; set; } = [];
}
