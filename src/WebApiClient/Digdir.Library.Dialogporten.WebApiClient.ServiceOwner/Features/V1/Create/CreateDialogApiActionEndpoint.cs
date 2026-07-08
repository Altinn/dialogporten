using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;

public class CreateDialogApiActionEndpoint
{
    /// <summary>
    /// A self-defined UUIDv7 may be provided to support idempotent creation of Api Action Endpoints. If not provided, a new UUIDv7 will be generated.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// Arbitrary string indicating the version of the endpoint.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// The fully qualified URL of the API endpoint.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = default!;

    /// <summary>
    /// The HTTP method that the endpoint expects for this action.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    [JsonConverter(typeof(JsonStringEnumConverter<HttpVerb>))]
    public HttpVerb HttpMethod { get; set; }

    /// <summary>
    /// Link to documentation for the endpoint, providing documentation for integrators. Should be a URL to a
    /// <br/>human-readable page.
    /// </summary>
    [JsonPropertyName("documentationUrl")]
    public Uri? DocumentationUrl { get; set; }

    /// <summary>
    /// Link to the request schema for the endpoint. Used to provide documentation for integrators.
    /// <br/>Dialogporten will not validate information on this endpoint.
    /// </summary>
    [JsonPropertyName("requestSchema")]
    public Uri? RequestSchema { get; set; }

    /// <summary>
    /// Link to the response schema for the endpoint. Used to provide documentation for integrators.
    /// <br/>Dialogporten will not validate information on this endpoint.
    /// </summary>
    [JsonPropertyName("responseSchema")]
    public Uri? ResponseSchema { get; set; }

    /// <summary>
    /// Boolean indicating if the endpoint is deprecated.
    /// </summary>
    [JsonPropertyName("deprecated")]
    public bool Deprecated { get; set; }

    /// <summary>
    /// Date and time when the endpoint will no longer function. Only set if the endpoint is deprecated. Dialogporten
    /// <br/>will not enforce this date.
    /// </summary>
    [JsonPropertyName("sunsetAt")]
    public DateTimeOffset? SunsetAt { get; set; }
}
