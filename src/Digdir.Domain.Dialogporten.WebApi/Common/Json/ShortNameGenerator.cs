using System.Reflection;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using NJsonSchema.Generation;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Json;

internal sealed class ShortNameGenerator(string documentName) : ISchemaNameGenerator
{
    private static bool UseOpenApiTypeNameAttribute(string documentName) => documentName is not "v1";

    public string Generate(Type type)
    {
        var currentName = TypeNameConverter.ToShortName(type);

        if (UseOpenApiTypeNameAttribute(documentName))
        {
            var attributeTypeName = type.GetCustomAttribute<OpenApiTypeNameAttribute>(inherit: false)?.TypeName;
            if (!string.IsNullOrWhiteSpace(attributeTypeName))
            {
                return attributeTypeName;
            }

            if (OpenApiTypeNameOverrides.TryGetOverride(documentName, currentName, out var overrideName))
            {
                return overrideName;
            }
        }

        OpenApiTypeNameOverrides.ThrowIfMissingOverride(documentName, type, currentName);
        return currentName;
    }
}
