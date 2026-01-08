using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using NJsonSchema;
using NSwag;

namespace Digdir.Domain.Dialogporten.WebApi;

public static class OpenApiDocumentExtensions
{
    /// <summary>
    /// FastEndpoints generates a title for headers with the format "System_String", which is a C# specific type.
    /// </summary>
    /// <param name="openApiDocument"></param>
    public static void RemoveSystemStringHeaderTitles(this OpenApiDocument openApiDocument)
    {
        const string systemString = "System_String";
        var headers = openApiDocument.Paths
            .SelectMany(path => path.Value
                .SelectMany(operation => operation.Value.Responses
                    .SelectMany(response => response.Value.Headers
                        .Where(header => header.Value.Schema.Title == systemString))));

        foreach (var header in headers)
        {
            header.Value.Schema.Title = null;
        }
    }

    /// <summary>
    /// To have this be validated in BlackDuck, we need to lower case the bearer scheme name.
    /// From editor.swagger.io:
    /// Structural error at components.securitySchemes.JWTBearerAuth
    /// should NOT have a `bearerFormat` property without `scheme: bearer` being set
    /// </summary>
    /// <param name="openApiDocument"></param>
    public static void FixJwtBearerCasing(this OpenApiDocument openApiDocument)
    {
        foreach (var securityScheme in openApiDocument.Components.SecuritySchemes.Values)
        {
            if (securityScheme.Scheme.Equals("Bearer", StringComparison.Ordinal))
            {
                securityScheme.Scheme = "bearer";
            }
        }
    }

    public static void AddServiceUnavailableResponse(this OpenApiDocument openApiDocument)
    {
        const string statusCode = "503";
        const string headerName = "Retry-After";

        foreach (var operation in openApiDocument.Paths.SelectMany(path => path.Value.Values))
        {
            if (!operation.Responses.TryGetValue(statusCode, out var response))
            {
                response = new OpenApiResponse
                {
                    Description = "Service Unavailable",
                    Content =
                    {
                        ["text/plain"] = new OpenApiMediaType
                        {
                            Schema = new JsonSchema
                            {
                                Type = JsonObjectType.String,
                                Example = "Service Unavailable"
                            }
                        }
                    }
                };

                operation.Responses[statusCode] = response;
            }

            if (!response.Headers.ContainsKey(headerName))
            {
                response.Headers[headerName] = new OpenApiHeader
                {
                    Description = "Delay before retrying the request.",
                    IsNullableRaw = true,
                    Schema = new JsonSchema
                    {
                        Type = JsonObjectType.String
                    }
                };
            }
        }
    }

    /// <summary>
    /// When generating ProblemDetails and ProblemDetails_Error, there is a bug/weird behavior in NSwag or FastEndpoints
    /// which results in certain 'Description' properties being generated when running on f.ex. MacOS,
    /// but not when running on the Ubuntu GitHub Actions runner. This leads to the OpenAPI swagger snapshot test
    /// behaving differently on different platforms/CPU architectures, which is not desirable.
    ///
    /// This method removes these descriptions.
    /// </summary>
    /// <param name="openApiDocument"></param>
    public static void ReplaceProblemDetailsDescriptions(this OpenApiDocument openApiDocument)
    {
        var schemas = openApiDocument.Components.Schemas;
        List<JsonSchema> schemaList = [schemas["ProblemDetails"], schemas["ProblemDetails_Error"]];

        foreach (var schema in schemaList)
        {
            if (schema.Description != null)
            {
                schema.Description = null;
            }

            if (schema.Properties == null)
            {
                continue;
            }

            foreach (var property in schema.Properties)
            {
                if (property.Value.Description != null)
                {
                    property.Value.Description = null;
                }
            }
        }
    }

    public static void MakeCollectionsNullable(this OpenApiDocument openApiDocument)
    {
        foreach (var schema in openApiDocument.Components.Schemas.Values)
        {
            MakeCollectionsNullable(schema);
        }
    }

    /// <summary>
    /// Changing the dialog status example to "NotApplicable" since the "New" status is deprecated.
    /// </summary>
    /// <param name="openApiDocument"></param>
    public static void ChangeDialogStatusExample(this OpenApiDocument openApiDocument)
    {
        if (!openApiDocument.Paths.TryGetValue("/api/v1/serviceowner/dialogs", out var pathItem))
        {
            return;
        }

        if (!pathItem.TryGetValue(OpenApiOperationMethod.Post, out var postOp))
        {
            return;
        }

        var requestBodyProperties = postOp.RequestBody?.Content?["application/json"]?.Schema?.ActualSchema?.ActualProperties;
        if (requestBodyProperties == null)
        {
            return;
        }
        if (requestBodyProperties.TryGetValue("status", out var statusProperty))
        {
            statusProperty.Example = nameof(DialogStatus.Values.NotApplicable);
        }
    }

    private static void MakeCollectionsNullable(JsonSchema schema)
    {
        if (schema.Properties == null)
        {
            return;
        }

        foreach (var property in schema.Properties.Values)
        {
            if (property.Type.HasFlag(JsonObjectType.Array))
            {
                property.IsNullableRaw = true;
            }
        }
    }
}
