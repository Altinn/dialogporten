namespace Digdir.Domain.Dialogporten.WebApi.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class OpenApiOperationIdAttribute(string operationId) : Attribute
{
    public string OperationId { get; } = operationId;
}
