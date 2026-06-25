using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Common;

public class ProblemDetailsError
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("paths")]
    public ICollection<string>? Paths { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
}

public class ProblemDetails
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    [JsonPropertyName("statusDescription")]
    public string? StatusDescription { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    [JsonPropertyName("validationErrors")]
    public ICollection<ProblemDetailsError>? ValidationErrors { get; set; }

    [JsonPropertyName("errors")]
    public IDictionary<string, ICollection<string>> Errors { get; set; } = null!;

    [JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
}
