using NJsonSchema;
using NSwag;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Swagger;

public static class OpenApiRequiredArraysPostProcessor
{
    public static void Process(OpenApiDocument document)
    {
        HashSet<JsonSchema> visitedSchemas = [];

        foreach (var schema in document.Components.Schemas.Values)
        {
            RemoveRequiredProperties(schema, visitedSchemas);
        }

        foreach (var operation in document.Paths.SelectMany(path => path.Value.Values))
        {
            foreach (var parameter in operation.Parameters)
            {
                RemoveRequiredProperties(parameter.Schema, visitedSchemas);
            }

            foreach (var schema in operation.RequestBody?.Content.Values
                         .Select(mediaType => mediaType.Schema)
                     ?? [])
            {
                RemoveRequiredProperties(schema, visitedSchemas);
            }

            foreach (var response in operation.Responses.Values)
            {
                foreach (var header in response.Headers.Values)
                {
                    RemoveRequiredProperties(header.Schema, visitedSchemas);
                }

                foreach (var schema in response.Content.Values.Select(mediaType => mediaType.Schema))
                {
                    RemoveRequiredProperties(schema, visitedSchemas);
                }
            }
        }
    }

    private static void RemoveRequiredProperties(JsonSchema? schema, HashSet<JsonSchema> visitedSchemas)
    {
        if (schema is null || !visitedSchemas.Add(schema))
        {
            return;
        }

        schema.RequiredProperties.Clear();

        if (schema.Reference is not null)
        {
            RemoveRequiredProperties(schema.Reference, visitedSchemas);
        }

        foreach (var property in schema.Properties.Values)
        {
            RemoveRequiredProperties(property, visitedSchemas);
        }

        foreach (var property in schema.PatternProperties.Values)
        {
            RemoveRequiredProperties(property, visitedSchemas);
        }

        foreach (var definition in schema.Definitions.Values)
        {
            RemoveRequiredProperties(definition, visitedSchemas);
        }

        foreach (var item in schema.Items)
        {
            RemoveRequiredProperties(item, visitedSchemas);
        }

        foreach (var schemaReference in schema.AllOf
                     .Concat(schema.AnyOf)
                     .Concat(schema.OneOf))
        {
            RemoveRequiredProperties(schemaReference, visitedSchemas);
        }

        RemoveRequiredProperties(schema.Item, visitedSchemas);
        RemoveRequiredProperties(schema.Not, visitedSchemas);
        RemoveRequiredProperties(schema.DictionaryKey, visitedSchemas);
        RemoveRequiredProperties(schema.AdditionalItemsSchema, visitedSchemas);
        RemoveRequiredProperties(schema.AdditionalPropertiesSchema, visitedSchemas);
    }
}
