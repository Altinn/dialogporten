using System.Reflection;
using Microsoft.AspNetCore.JsonPatch;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Json;

// Ensures NSwag advertises both JSON Patch and plain JSON media types for JsonPatchDocument request bodies.
// Plain JSON is kept for backwards compatibility and should be removed in V2.
// https://github.com/Altinn/dialogporten/issues/3321
// NSwag does not automatically pick up [Consume] for this MVC controller scenario.
public sealed class JsonPatchConsumesOperationProcessor : IOperationProcessor
{
    private const string JsonMediaType = "application/json";
    private const string JsonPatchMediaType = "application/json-patch+json";

    public bool Process(OperationProcessorContext context)
    {
        if (!HasJsonPatchDocument(context.MethodInfo))
        {
            return true;
        }

        var requestBody = context.OperationDescription.Operation.RequestBody;
        if (requestBody?.Content is null || requestBody.Content.Count == 0)
        {
            return true;
        }

        var content = requestBody.Content;
        if (!content.TryGetValue(JsonMediaType, out var mediaType)
            && !content.TryGetValue(JsonPatchMediaType, out mediaType))
        {
            mediaType = content.First().Value;
        }

        content.TryAdd(JsonMediaType, mediaType);
        content.TryAdd(JsonPatchMediaType, mediaType);
        return true;
    }

    private static bool HasJsonPatchDocument(MethodInfo methodInfo)
        => methodInfo.GetParameters().Any(parameter => IsJsonPatchDocumentType(parameter.ParameterType));

    private static bool IsJsonPatchDocumentType(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(JsonPatchDocument<>);
}
