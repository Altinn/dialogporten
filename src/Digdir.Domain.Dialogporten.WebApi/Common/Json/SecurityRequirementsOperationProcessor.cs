using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using NSwag;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using Serilog;
using AuthorizationPolicy = Digdir.Domain.Dialogporten.WebApi.Common.Authorization.AuthorizationPolicy;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Json;

public sealed class SecurityRequirementsOperationProcessor : IOperationProcessor
{
    public const string IdportenSecurityScheme = "Idporten";
    public const string MaskinportenSecurityScheme = "Maskinporten";
    public const string FastEndpointsDefaultSecurityScheme = "JWTBearerAuth";

    public bool Process(OperationProcessorContext context)
    {
        var operationSecurity = context.OperationDescription.Operation.Security;
        if (operationSecurity is null) return true;
        var securityRequirement = operationSecurity.FirstOrDefault();
        if (securityRequirement == null || !securityRequirement.TryGetValue(FastEndpointsDefaultSecurityScheme, out var value))
        {
            return true;
        }

        var aspNetContext = (AspNetCoreOperationProcessorContext)context;
        var policy = aspNetContext.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Select(x => x.Policy)
            .FirstOrDefault(x => !string.IsNullOrEmpty(x));

        if (policy is null)
        {
            securityRequirement[MaskinportenSecurityScheme] = value;
            securityRequirement.Remove(FastEndpointsDefaultSecurityScheme);
            return true;
        }

        if (!AuthorizationOptionsSetup.ScopeRulesByPolicy.TryGetValue(policy, out var scopeRules))
        {
            var logger = Log.ForContext<SecurityRequirementsOperationProcessor>();
            logger.Error(
                "Can't determine scope for endpoint {Method} {Endpoint}. Policy: {Policy}. Check the PolicyScopeMap",
                aspNetContext.ApiDescription.HttpMethod,
                aspNetContext.ApiDescription.RelativePath,
                policy
            );
            securityRequirement[MaskinportenSecurityScheme] = value;
            securityRequirement.Remove(FastEndpointsDefaultSecurityScheme);
            return true;
        }

        operationSecurity = operationSecurity.Skip(1).ToList();

        foreach (var rule in scopeRules)
        {
            operationSecurity.Add(rule, MaskinportenSecurityScheme);
            if (policy == AuthorizationPolicy.EndUser)
            {
                operationSecurity.Add(rule, IdportenSecurityScheme);
            }
        }

        context.OperationDescription.Operation.Security = operationSecurity;
        return true;
    }
}
