using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Json;

/// <summary>
/// FastEndPoints.Swagger generates complex types for the ContinuationToken and OrderBy parameters, while these are
/// (like most query parameters) just strings. This processor replaces the schema for these parameters with a string.
/// This does not however, remove the complex types from the generated documentation, these are handlied by the
/// schema processor in PaginationAndOrderingsSchemaProcessor.cs
/// </summary>
public sealed class PaginatedListParametersProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        foreach (var parameter in context.OperationDescription.Operation.Parameters)
        {
            if (parameter.Kind != OpenApiParameterKind.Query) continue;
            if (parameter.Name is nameof(PaginatedList<>.ContinuationToken) or nameof(PaginatedList<>.OrderBy))
            {
                parameter.Schema = new JsonSchema { Type = JsonObjectType.String, IsNullableRaw = true };
            }
        }

        return true;
    }
}
