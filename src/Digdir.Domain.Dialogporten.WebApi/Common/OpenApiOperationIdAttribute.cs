namespace Digdir.Domain.Dialogporten.WebApi.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class OpenApiOperationIdAttribute : Attribute
{
    public string OperationId { get; }

    public OpenApiOperationIdAttribute(string operationId)
    {
        OperationId = operationId;
    }
}
