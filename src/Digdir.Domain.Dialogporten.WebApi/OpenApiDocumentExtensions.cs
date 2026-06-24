using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.WebApi.Common.Json;
using Microsoft.AspNetCore.Authorization;
using NJsonSchema;
using NSwag;
using static Digdir.Domain.Dialogporten.WebApi.Common.Json.SecurityRequirementsOperationProcessor;

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

    /// <summary>
    /// Replaces/Adds the security schemes with hard-coded values instead of letting refitter generate them.
    /// </summary>
    /// <param name="openApiDocument"></param>
    /// <param name="settings"></param>
    public static void UpsertSecuritySchemes(
        this OpenApiDocument openApiDocument,
        DialogportenSwaggerUiSettings settings
    )
    {
        openApiDocument.Components.SecuritySchemes[IdPortenSecurityScheme] = new OpenApiSecurityScheme
        {
            ExtensionData = null,
            Type = OpenApiSecuritySchemeType.OAuth2,
            Name = "ID-Porten",
            Description = $"""
                          Browser login using ID-Porten (OIDC).
                          - You can obtain a token from ID-Porten using the Authorization Code flow.
                          - This token can be only be used with the Enduser endpoints.
                          
                          Scopes:
                          - {AuthorizationScope.EndUser}
                          - {AuthorizationScope.EndUserNoConsent}
                          """,
            In = OpenApiSecurityApiKeyLocation.Header,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = settings.IdportenAuthorizationUrl,
                    TokenUrl = settings.IdportenTokenUrl,
                    RefreshUrl = null,
                    Scopes = new Dictionary<string, string>
                    {
                        ["openid"] = "Access to core user information",
                        ["profile"] = "Access to user profile details",
                        [AuthorizationScope.EndUser] = "Access to dialogporten",
                        [AuthorizationScope.EndUserNoConsent] = "Access to dialogporten",
                    }
                }
            },
        };
        openApiDocument.Components.SecuritySchemes[JwtBearerAuth] = new OpenApiSecurityScheme
        {
            ExtensionData = null,
            Type = OpenApiSecuritySchemeType.Http,
            Name = "Maskinporten",
            Description = $"""
                          Machine login using Maskinporten.
                          - You can obtain a token from Maskinporten using JWT Bearer Grant (RFC 7523).
                          - This token can be used for all secured endpoints.
                          - To use this token with the Enduser API's, you have to register a "system" and "system user".
                          - Required scopes are listed per endpoint as Security Requirement Objects.
                          
                          Please refer to the Dialogporten documentation for how to get tokens and register system users.
                          Scopes:
                          - {AuthorizationScope.ServiceProvider}
                          - {AuthorizationScope.ServiceProviderSearch}
                          - {AuthorizationScope.ServiceProviderChangeTransmissions}
                          """,
            In = OpenApiSecurityApiKeyLocation.Header,
            Scheme = "bearer",
            BearerFormat = "JWT",
            TokenUrl = settings.MaskinportenTokenUrl,
        };
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
