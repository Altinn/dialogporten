using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Microsoft.AspNetCore.Mvc;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common;

[OpenApiTypeName("ConflictProblemDetails")]
public sealed class ConflictProblemDetails(IDictionary<string, string[]> errors) : ValidationProblemDetails(errors)
{
    public List<Conflict> Conflicts { get; set; } = [];
}

[OpenApiTypeName("Conflict")]
public sealed class Conflict
{
    public required string Key { get; set; }
    [OneOfTypes(typeof(string), typeof(int))]
    public required object Value { get; set; }
    public required string Reason { get; set; }
}
