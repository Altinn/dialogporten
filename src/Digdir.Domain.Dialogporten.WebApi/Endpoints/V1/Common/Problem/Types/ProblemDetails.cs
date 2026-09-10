using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Microsoft.AspNetCore.Mvc;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Types;

public interface IDialogportenProblemDetails
{
    // Fields from Microsoft.AspNetCore.Mvc.ProblemDetails
    string? Type { get; set; }
    string? Title { get; set; }
    int? Status { get; set; }
    string? Detail { get; set; }
    string? Instance { get; set; }

    // Fields from Digdir standard
    string? TraceId { get; set; }
    string? StatusDescription { get; set; }
    string? Code { get; set; }
    List<ProblemDetails_Error>? ValidationErrors { get; set; }

    // Fields from Microsoft.AspNetCore.Mvc.ValidationProblemDetails
    Dictionary<string, string[]> Errors { get; set; }
}

[OpenApiTypeName("ProblemDetails")]
public sealed class ProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails, IDialogportenProblemDetails
{
    public string? TraceId { get; set; }
    public string? StatusDescription { get; set; }
    public string? Code { get; set; }
    public List<ProblemDetails_Error>? ValidationErrors { get; set; }
    public Dictionary<string, string[]> Errors { get; set; } = new(StringComparer.Ordinal);
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
