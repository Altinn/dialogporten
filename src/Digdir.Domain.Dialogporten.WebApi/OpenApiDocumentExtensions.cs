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

    /// <summary>
    /// Adds descriptions to the top-level tags (endpoint groups). These are rendered
    /// underneath the group heading in the OpenAPI reference UI (e.g. Scalar/Swagger).
    /// Descriptions support Markdown. Only tags actually used by an operation in the
    /// document get a description, so audience-specific documents don't gain empty sections.
    /// </summary>
    public static void AddTagDescriptions(this OpenApiDocument openApiDocument)
    {
        var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Serviceowner"] =
                "Endpoints for service owners to create and manage dialogs. " +
                "Requires a Maskinporten token with the relevant `digdir:dialogporten.serviceprovider` scope. " +
                "The search endpoint additionally requires the `digdir:dialogporten.serviceprovider.search` scope.",
            ["Enduser"] =
                "Endpoints for end users to read and act on dialogs they are authorized to access. " +
                "Used both by persons logged in via ID-porten and by Altinn system users authenticated via Maskinporten. " +
                "Requires a token with the `digdir:dialogporten` scope (or `digdir:dialogporten.noconsent`).",
            ["Metadata"] =
                "Public, unauthenticated metadata endpoints such as health and configuration information."
        };

        var usedTagNames = openApiDocument.Paths
            .SelectMany(path => path.Value.Values)
            .SelectMany(operation => operation.Tags)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, description) in descriptions)
        {
            if (!usedTagNames.Contains(name))
            {
                continue;
            }

            var tag = openApiDocument.Tags
                .FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

            if (tag is null)
            {
                tag = new OpenApiTag { Name = name };
                openApiDocument.Tags.Add(tag);
            }

            tag.Description = description;
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
