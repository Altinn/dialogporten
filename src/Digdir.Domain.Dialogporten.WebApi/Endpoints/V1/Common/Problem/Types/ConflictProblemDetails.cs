using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.WebApi.Common;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Types;

[OpenApiTypeName("ConflictProblemDetails")]
public sealed class ConflictProblemDetails : ProblemDetails
{
    public List<ProblemDetailsConflict> Conflicts { get; set; } = [];
}

public class ProblemDetailsConflict
{
    public required string Code { get; init; }
    public required string Title { get; init; }

    [JsonExtensionData]
    public IDictionary<string, object?> Extensions { get; set; } = new Dictionary<string, object?>(
        StringComparer.Ordinal
    );
}
