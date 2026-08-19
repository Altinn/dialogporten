using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using Serilog;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.OpenApiSecurityScheme;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Json;

public sealed class SecurityRequirementsOperationProcessor : IOperationProcessor
{
    public const string FastEndpointsSecurityScheme = "JWTBearerAuth";

    /// <summary>
    /// Renames the JWTBearerAuth security scheme, added by fastendpoints to "maskinporten".
    /// Adds a new security scheme "idporten" to the enduser apis.
    /// Relinks all endpoints to use the correct schemes.
    ///
    /// </summary>
    /// <returns></returns>
    public bool Process(OperationProcessorContext context)
    {
        var operationSecurity = context.OperationDescription.Operation.Security;
        if (operationSecurity is null) return true;

        var securityRequirement = operationSecurity.FirstOrDefault(x => x.ContainsKey(FastEndpointsSecurityScheme));
        if (securityRequirement == null) return true;
        if (!securityRequirement.TryGetValue(FastEndpointsSecurityScheme, out var existingScheme)) return true;

        var aspNetContext = (AspNetCoreOperationProcessorContext)context;
        var additionalMetadata = aspNetContext.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<OpenApiExtrasAttribute>()
            .Concat(aspNetContext.ApiDescription.ActionDescriptor.EndpointMetadata
                .OfType<EndpointDefinition>()
                .FirstOrDefault()?
                .EndpointAttributes?
                .OfType<OpenApiExtrasAttribute>() ?? [])
            .FirstOrDefault();

        if (additionalMetadata is null)
        {
            var logger = Log.ForContext<SecurityRequirementsOperationProcessor>();
            logger.Error(
                "Missing metadata for endpoint {Method} {Endpoint}. Add {Attribute} to the endpoint",
                aspNetContext.ApiDescription.HttpMethod,
                aspNetContext.ApiDescription.RelativePath,
                nameof(OpenApiExtrasAttribute)
            );
            securityRequirement[MaskinportenSecurityScheme] = existingScheme;
            securityRequirement.Remove(FastEndpointsSecurityScheme);
            return true;
        }

        operationSecurity = operationSecurity
            .Where(x => !x.ContainsKey(FastEndpointsSecurityScheme))
            .ToList();

        var endpointSecuritySchemes = additionalMetadata.SecuritySchemes;
        var endpointRequiredScopes = additionalMetadata.Scopes;

        if (endpointSecuritySchemes.Contains(MaskinportenSecurityScheme))
        {
            operationSecurity.Add(endpointRequiredScopes, MaskinportenSecurityScheme);
        }
        if (endpointSecuritySchemes.Contains(IdportenSecurityScheme))
        {
            operationSecurity.Add(endpointRequiredScopes, IdportenSecurityScheme);
        }

        if (operationSecurity.Count == 0)
        {
            throw new ArgumentException($"Could not find any known security schemes in {endpointSecuritySchemes}");
        }

        context.OperationDescription.Operation.Security = operationSecurity;
        return true;
    }
}
