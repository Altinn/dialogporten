using NJsonSchema;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Patch;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ProducesResponseHeaderAttribute : Attribute
{
    public ProducesResponseHeaderAttribute(int statusCode, string headerName,
        string description, string example, JsonObjectType type = JsonObjectType.String)
    {
        HeaderName = headerName;
        StatusCode = statusCode;
        Description = description;
        Example = example;
        Type = type;
    }

    public string HeaderName { get; }
    public int StatusCode { get; }
    public string Description { get; }
    public string Example { get; }

    public JsonObjectType Type { get; }
}
