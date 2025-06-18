using System.Globalization;
using System.Reflection;
using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Patch;

public sealed class ProducesResponseHeaderOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var headerAttribute = context.MethodInfo.GetCustomAttribute<ProducesResponseHeaderAttribute>();
        if (headerAttribute == null)
        {
            return true;
        }

        var statusCode = headerAttribute.StatusCode.ToString(CultureInfo.InvariantCulture);
        var response = context.OperationDescription.Operation.Responses[statusCode];
        var header = new OpenApiHeader
        {
            Description = headerAttribute.Description,
            Example = headerAttribute.Example,
            Schema = new JsonSchema
            {
                Type = headerAttribute.Type
            }
        };

        response.Headers.Add(headerAttribute.HeaderName, header);
        return true;
    }
}
