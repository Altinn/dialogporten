using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.WebApi.Common.Json;
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
                    Description = "Service Unavailable, used when Dialogporten is in maintenance mode",
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
                    Description = "Delay before retrying the request. Datetime format RFC1123",
                    Schema = new JsonSchema
                    {
                        Type = JsonObjectType.String
                    }
                };
            }
        }
    }

    /// <summary>
    /// Replaces the auto-generated ProblemDetails schema (originating from FastEndpoints' OOTB
    /// type) with one that matches what the runtime actually returns from
    /// <see cref="Common.Extensions.ErrorResponseBuilderExtensions"/>; i.e.
    /// <see cref="Common.Errors.DialogportenProblemDetails"/>. Also drops the now-orphaned
    /// ProblemDetails_Error schema and registers a new ValidationError schema.
    /// </summary>
    public static void ReplaceProblemDetailsSchema(this OpenApiDocument openApiDocument)
    {
        var schemas = openApiDocument.Components.Schemas;
        var problemDetailsSchema = schemas["ProblemDetails"];

        schemas.Remove("ProblemDetails_Error");

        var validationErrorSchema = BuildValidationErrorSchema();
        schemas["ValidationError"] = validationErrorSchema;
        ConfigureProblemDetailsSchema(problemDetailsSchema, validationErrorSchema);
    }

    private static JsonSchema BuildValidationErrorSchema()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            AllowAdditionalProperties = false
        };
        schema.Properties["code"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Properties["detail"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Properties["paths"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Array,
            Item = new JsonSchema { Type = JsonObjectType.String }
        };
        return schema;
    }

    private static void ConfigureProblemDetailsSchema(JsonSchema schema, JsonSchema validationErrorSchema)
    {
        schema.Type = JsonObjectType.Object;
        schema.AllowAdditionalProperties = false;
        schema.Description = null;
        schema.Properties.Clear();
        schema.RequiredProperties.Clear();

        schema.Properties["type"] = new JsonSchemaProperty { Type = JsonObjectType.String, IsNullableRaw = true };
        schema.Properties["title"] = new JsonSchemaProperty { Type = JsonObjectType.String, IsNullableRaw = true };
        schema.Properties["status"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Integer,
            Format = JsonFormatStrings.Integer,
            IsNullableRaw = true
        };
        schema.Properties["detail"] = new JsonSchemaProperty { Type = JsonObjectType.String, IsNullableRaw = true };
        schema.Properties["instance"] = new JsonSchemaProperty { Type = JsonObjectType.String, IsNullableRaw = true };
        schema.Properties["code"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Properties["statusDescription"] = new JsonSchemaProperty { Type = JsonObjectType.String, IsNullableRaw = true };
        schema.Properties["errors"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Object,
            IsNullableRaw = true,
            AdditionalPropertiesSchema = new JsonSchema
            {
                Type = JsonObjectType.Array,
                Item = new JsonSchema { Type = JsonObjectType.String }
            }
        };
        schema.Properties["validationErrors"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Array,
            IsNullableRaw = true,
            Item = new JsonSchema { Reference = validationErrorSchema }
        };
        schema.Properties["problems"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Array,
            IsNullableRaw = true,
            Item = new JsonSchema { Reference = schema }
        };
        schema.Properties["traceId"] = new JsonSchemaProperty { Type = JsonObjectType.String, IsNullableRaw = true };
    }

    public static void MakeCollectionsNullable(this OpenApiDocument openApiDocument)
    {
        foreach (var schema in openApiDocument.Components.Schemas.Values)
        {
            MakeCollectionsNullable(schema);
        }
    }

    public static void RemoveRequiredPropertiesFromSchemas(this OpenApiDocument openApiDocument)
    {
        foreach (var schema in openApiDocument.Components.Schemas.Values)
        {
            schema.RequiredProperties.Clear();
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

    /// <summary>
    /// NSwag generates empty schema definitions for the generic pagination types
    /// (ContinuationTokenSet, OrderSet) that are not useful in the OpenAPI spec.
    /// The parameters themselves are replaced with string schemas by <see cref="PaginatedListParametersProcessor"/>,
    /// so these schema definitions are unreferenced and should be removed.
    /// </summary>
    public static void RemoveUnusedPaginationSchemas(this OpenApiDocument openApiDocument)
    {
        openApiDocument.Components.Schemas.Remove("ContinuationTokenSetOfTOrderDefinitionAndTTarget");
        openApiDocument.Components.Schemas.Remove("OrderSetOfTOrderDefinitionAndTTarget");
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
