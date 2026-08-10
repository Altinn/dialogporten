using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using Serilog;
using AuthorizationPolicy = Digdir.Domain.Dialogporten.WebApi.Common.Authorization.AuthorizationPolicy;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Json;

public sealed class SecurityRequirementsOperationProcessor : IOperationProcessor
{
    public const string IdportenSecurityScheme = "idporten";
    public const string MaskinportenSecurityScheme = "maskinporten";
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
        var policy = aspNetContext.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Select(x => x.Policy)
            .FirstOrDefault(x => !string.IsNullOrEmpty(x));

        if (policy is null)
        {
            securityRequirement[MaskinportenSecurityScheme] = existingScheme;
            securityRequirement.Remove(FastEndpointsSecurityScheme);
            return true;
        }

        if (!AuthorizationOptionsSetup.ScopeRulesByPolicy.TryGetValue(policy, out var scopes))
        {
            var logger = Log.ForContext<SecurityRequirementsOperationProcessor>();
            logger.Error(
                "Can't determine scope for endpoint {Method} {Endpoint}. Policy: {Policy}. Check the PolicyScopeMap",
                aspNetContext.ApiDescription.HttpMethod,
                aspNetContext.ApiDescription.RelativePath,
                policy
            );
            securityRequirement[MaskinportenSecurityScheme] = existingScheme;
            securityRequirement.Remove(FastEndpointsSecurityScheme);
            return true;
        }

        operationSecurity = operationSecurity
            .Where(x => !x.ContainsKey(FastEndpointsSecurityScheme))
            .ToList();

        operationSecurity.Add(scopes, MaskinportenSecurityScheme);
        if (policy == AuthorizationPolicy.EndUser)
        {
            operationSecurity.Add(scopes, IdportenSecurityScheme);
        }

        context.OperationDescription.Operation.Security = operationSecurity;
        return true;
    }
}
