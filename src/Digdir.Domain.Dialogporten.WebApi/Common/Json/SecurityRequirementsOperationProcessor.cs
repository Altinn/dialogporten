using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner;

using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Json;

public sealed class SecurityRequirementsOperationProcessor : IOperationProcessor
{
    private const string JwtBearerAuth = "JWTBearerAuth";
    private const string ServiceOwnerSearchPath = "/api/v1/serviceowner/dialogs";

    public bool Process(OperationProcessorContext context)
    {
        var securityRequirement = context.OperationDescription.Operation.Security?.FirstOrDefault();
        if (securityRequirement == null || !securityRequirement.TryGetValue(JwtBearerAuth, out var value))
        {
            return true;
        }

        securityRequirement[JwtBearerAuth] =
            (context.OperationDescription.Operation.Tags.FirstOrDefault()?.ToLowerInvariant()) switch
            {
                ServiceOwnerGroup.RoutePrefix => IsServiceOwnerSearchEndpoint(context)
                    ? [AuthorizationScope.ServiceProvider, AuthorizationScope.ServiceProviderSearch]
                    : [AuthorizationScope.ServiceProvider],
                EndUserGroup.RoutePrefix => [AuthorizationScope.EndUser],
                _ => value
            };

        return true;
    }

    private static bool IsServiceOwnerSearchEndpoint(OperationProcessorContext context)
        => context.OperationDescription.Path == ServiceOwnerSearchPath;
}
