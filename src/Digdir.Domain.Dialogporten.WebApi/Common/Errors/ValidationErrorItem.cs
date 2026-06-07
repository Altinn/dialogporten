using System.Text.Json.Serialization;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Errors;

public sealed record ValidationErrorItem(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("detail")] string Detail,
    [property: JsonPropertyName("paths")] IReadOnlyList<string> Paths);
