using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Action;

public class DialogApiActionEndpoint
{
    /// <summary>
    /// The unique identifier for the endpoint in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Arbitrary string indicating the version of the endpoint.
    /// <br/>
    /// <br/>Consult the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// The fully qualified URL of the API endpoint. Will be set to "urn:dialogporten:unauthorized" if the user is
    /// <br/>not authorized to perform the action.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The HTTP method that the endpoint expects for this action.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    [JsonConverter(typeof(JsonStringEnumConverter<HttpVerb>))]
    public HttpVerb HttpMethod { get; set; }

    /// <summary>
    /// Link to service provider documentation for the endpoint. Used for service owners to provide documentation for
    /// <br/>integrators. Should be a URL to a human-readable page.
    /// </summary>
    [JsonPropertyName("documentationUrl")]
    public Uri? DocumentationUrl { get; set; }

    /// <summary>
    /// Link to the request schema for the endpoint. Used by service owners to provide documentation for integrators.
    /// <br/>Dialogporten will not validate information on this endpoint.
    /// </summary>
    [JsonPropertyName("requestSchema")]
    public Uri? RequestSchema { get; set; }

    /// <summary>
    /// Link to the response schema for the endpoint. Used for service owners to provide documentation for integrators.
    /// <br/>Dialogporten will not validate information on this endpoint.
    /// </summary>
    [JsonPropertyName("responseSchema")]
    public Uri? ResponseSchema { get; set; }

    /// <summary>
    /// Boolean indicating if the endpoint is deprecated. Integrators should migrate to endpoints with a higher version.
    /// </summary>
    [JsonPropertyName("deprecated")]
    public bool Deprecated { get; set; }

    /// <summary>
    /// Date and time when the service owner has indicated that endpoint will no longer function. Only set if the endpoint
    /// <br/>is deprecated. Dialogporten will not enforce this date.
    /// </summary>
    [JsonPropertyName("sunsetAt")]
    public DateTimeOffset? SunsetAt { get; set; }
}