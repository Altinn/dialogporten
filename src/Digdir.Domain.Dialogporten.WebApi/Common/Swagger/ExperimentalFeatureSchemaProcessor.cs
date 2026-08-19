using System.Collections.Concurrent;
using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using NJsonSchema;
using NJsonSchema.Generation;
using NSwag;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Swagger;

/// <summary>
/// Flags schemas generated from types marked with <see cref="ExperimentalFeatureAttribute"/>:
/// the schema description gets a standardized experimental notice and an "x-experimental" vendor
/// extension. <see cref="AddPropertyNotices"/> then propagates the notice to every property
/// referencing a flagged schema, so contract types only need the attribute in one place.
/// </summary>
internal sealed class ExperimentalFeatureSchemaProcessor : ISchemaProcessor
{
    private const string ExtensionName = "x-experimental";
    private readonly ConcurrentDictionary<JsonSchema, string> _noticesBySchema = new(ReferenceEqualityComparer.Instance);

    public void Process(SchemaProcessorContext context)
    {
        var attribute = context.ContextualType.Type.GetCustomAttribute<ExperimentalFeatureAttribute>();
        if (attribute is null)
        {
            return;
        }

        var notice =
            "**Experimental:** This is part of an experimental feature that may change or be removed " +
            $"without a major version bump. See {attribute.DocumentationUrl} for details.";

        MarkExperimental(context.Schema, notice);
        _noticesBySchema[context.Schema] = notice;
    }

    /// <summary>
    /// Document post-process step: prepends the experimental notice to every property whose type
    /// (or array item type) resolves to a schema flagged by <see cref="Process"/>.
    /// </summary>
    public void AddPropertyNotices(OpenApiDocument document)
    {
        var properties = document.Components.Schemas.Values
            .SelectMany(schema => schema.ActualProperties.Values);

        foreach (var property in properties)
        {
            // A bare $ref property cannot carry its own description or extensions. Properties
            // wrapping their reference in oneOf/allOf (HasReference is also true for those) can.
            if (property.Reference is not null)
            {
                continue;
            }

            if (TryGetNotice(property, out var notice) ||
                (property.Item is not null && TryGetNotice(property.Item, out notice)))
            {
                MarkExperimental(property, notice);
            }
        }
    }

    // ActualTypeSchema resolves a oneOf/allOf wrapper to the reference-holding item, which still
    // needs ActualSchema to land on the flagged component schema.
    private bool TryGetNotice(JsonSchema schema, out string notice) =>
        _noticesBySchema.TryGetValue(schema.ActualTypeSchema, out notice!) ||
        _noticesBySchema.TryGetValue(schema.ActualTypeSchema.ActualSchema, out notice!);

    private static void MarkExperimental(JsonSchema schema, string notice)
    {
        schema.Description = string.IsNullOrEmpty(schema.Description)
            ? notice
            : $"{notice}\n\n{schema.Description}";
        schema.ExtensionData ??= new Dictionary<string, object?>();
        schema.ExtensionData[ExtensionName] = true;
    }
}
