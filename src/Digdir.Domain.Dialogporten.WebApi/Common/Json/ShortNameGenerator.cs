using System.Reflection;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using NJsonSchema.Generation;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Json;

internal sealed class ShortNameGenerator(string documentName) : ISchemaNameGenerator
{
    public string Generate(Type type)
    {
        var attributeTypeName = type.GetCustomAttribute<OpenApiTypeNameAttribute>(inherit: false)?.TypeName;
        if (!string.IsNullOrWhiteSpace(attributeTypeName))
        {
            return attributeTypeName;
        }

        var currentName = TypeNameConverter.ToShortName(type);
        return OpenApiTypeNameOverrides.TryGetOverride(documentName, currentName, out var overrideName)
            ? overrideName
            : currentName;
    }
}
