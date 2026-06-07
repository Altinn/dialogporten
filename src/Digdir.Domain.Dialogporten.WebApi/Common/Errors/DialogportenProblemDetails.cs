using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Errors;

public sealed class DialogportenProblemDetails : ProblemDetails
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("statusDescription")]
    public string? StatusDescription { get; set; }

    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; set; }

    [JsonPropertyName("validationErrors")]
    public IReadOnlyList<ValidationErrorItem>? ValidationErrors { get; set; }

    [JsonPropertyName("problems")]
    public IReadOnlyList<DialogportenProblemDetails>? Problems { get; set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }
}
