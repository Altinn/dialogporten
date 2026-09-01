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
        var type = context.ContextualType.Type;

        if (type.GetCustomAttribute<ExperimentalFeatureAttribute>() is { } typeAttribute)
        {
            var notice = BuildNotice(typeAttribute);
            MarkExperimental(context.Schema, notice);
            _noticesBySchema[context.Schema] = notice;
        }

        MarkExperimentalProperties(type, context.Schema);
    }

    /// <summary>
    /// Flags the schema properties generated from marked CLR properties. Needed for members that carry no
    /// contract type of their own - a token string, a boolean flag - which <see cref="AddPropertyNotices"/>
    /// cannot reach, as it works off the schema a property references.
    /// </summary>
    private static void MarkExperimentalProperties(Type type, JsonSchema schema)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<ExperimentalFeatureAttribute>() is not { } attribute)
            {
                continue;
            }

            // The schema property is keyed by its serialized name; the contract uses camel casing
            // throughout, so a case-insensitive lookup is enough to pair the two.
            var schemaProperty = schema.ActualProperties
                .FirstOrDefault(x => string.Equals(x.Key, property.Name, StringComparison.OrdinalIgnoreCase))
                .Value;

            if (schemaProperty is not null)
            {
                MarkExperimental(schemaProperty, BuildNotice(attribute));
            }
        }
    }

    private static string BuildNotice(ExperimentalFeatureAttribute attribute) =>
        "**Experimental:** This is part of an experimental feature that may change or be removed " +
        $"without a major version bump. See {attribute.DocumentationUrl} for details.";

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
        // A property can be reached twice - marked directly and again through the flagged schema it
        // references - and the notice must not be prepended twice.
        if (schema.ExtensionData?.ContainsKey(ExtensionName) == true)
        {
            return;
        }

        schema.Description = string.IsNullOrEmpty(schema.Description)
            ? notice
            : $"{notice}\n\n{schema.Description}";
        schema.ExtensionData ??= new Dictionary<string, object?>();
        schema.ExtensionData[ExtensionName] = true;
    }
}
