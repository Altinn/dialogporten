using NSwag;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Swagger;

public static class OpenApiRequiredArraysPostProcessor
{
    public static void Process(OpenApiDocument document)
    {
        foreach (var schema in document.Components.Schemas.Values)
        {
            schema.RequiredProperties.Clear();
        }
    }
}
