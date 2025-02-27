using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner;
using NSwag;
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
            context.OperationDescription.Operation.Tags.FirstOrDefault() switch
            {
                var tag when string.Equals(tag, ServiceOwnerGroup.RoutePrefix, StringComparison.OrdinalIgnoreCase)
                    => IsServiceOwnerSearchEndpoint(context.OperationDescription)
                        ? new[] { AuthorizationScope.ServiceProvider, AuthorizationScope.ServiceProviderSearch }
                        : new[] { AuthorizationScope.ServiceProvider },

                var tag when string.Equals(tag, EndUserGroup.RoutePrefix, StringComparison.OrdinalIgnoreCase)
                    => new[] { AuthorizationScope.EndUser },

                _ => value
            };

        return true;
    }

    private static bool IsServiceOwnerSearchEndpoint(OpenApiOperationDescription description)
        => description is { Path: ServiceOwnerSearchPath, Method: OpenApiOperationMethod.Get };
}
