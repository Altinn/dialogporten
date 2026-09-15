using System.Reflection;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Swagger;

[AttributeUsage(AttributeTargets.Property)]
public sealed class OneOfTypesAttribute : Attribute
{
    public Type[] Types { get; }
    public OneOfTypesAttribute(params Type[] types)
    {
        Types = types;
    }
}

public sealed class OneOfTypesSchemaProcessor : ISchemaProcessor
{
    public void Process(SchemaProcessorContext context)
    {
        var properties = context.ContextualType.Type
            .GetProperties()
            .Select(p => (p, attr: p.GetCustomAttribute<OneOfTypesAttribute>()))
            .Where(x => x.attr != null);

        foreach (var (propInfo, attr) in properties)
        {
            if (string.IsNullOrEmpty(propInfo.Name)) continue;
            var name = propInfo.Name.Length == 1
                ? char.ToLowerInvariant(propInfo.Name[0]).ToString()
                : char.ToLowerInvariant(propInfo.Name[0]) + propInfo.Name[1..];

            if (!context.Schema.Properties.TryGetValue(name, out var prop)) continue;

            prop.Type = JsonObjectType.None;
            prop.Reference = null;
            prop.OneOf.Clear();

            foreach (var type in attr!.Types)
            {
                var schema = context.Generator.Generate(type, context.Resolver);
                prop.OneOf.Add(schema);
            }
        }
    }
}
