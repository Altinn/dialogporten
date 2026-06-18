using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.WebApi.Common;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common;

[OpenApiTypeName("ProblemDetails")]
public sealed class ProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
{
    public string? StatusDescription { get; set; }
    public string? Code { get; set; }
    public string? TraceId { get; set; }
    public List<ProblemDetails_Error>? ValidationErrors { get; set; }

    public Dictionary<string, string[]> Errors { get; set; } = [];
}

[OpenApiTypeName("ProblemDetails_Error")]
#pragma warning disable CA1707
public sealed class ProblemDetails_Error
#pragma warning restore CA1707
{
    public string? Title { get; set; }
    public string? Code { get; set; }
    public string? Detail { get; set; }

    public string[] Paths { get; set; } = [];

    [JsonExtensionData]
    public IDictionary<string, object?> Extensions { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
