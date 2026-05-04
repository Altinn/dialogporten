namespace Digdir.Domain.Dialogporten.WebApi.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
public sealed class OpenApiTypeNameAttribute(string typeName) : Attribute
{
    public string TypeName { get; } = typeName;
}
