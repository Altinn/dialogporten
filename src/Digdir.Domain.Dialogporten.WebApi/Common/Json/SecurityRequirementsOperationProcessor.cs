using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Json;

public sealed class SecurityRequirementsOperationProcessor : IOperationProcessor
{
    public const string IdPortenSecurityScheme = "IdPorten";
    public const string JwtBearerAuth = "JWTBearerAuth";
    private static readonly HashSet<string> ServiceOwnerSearchPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/serviceowner/dialogs",
        "/api/v1/serviceowner/dialogs/endusercontext"
    };

    public bool Process(OperationProcessorContext context)
    {
        var securityRequirement = context.OperationDescription.Operation.Security?.FirstOrDefault();
        if (securityRequirement == null || !securityRequirement.TryGetValue(JwtBearerAuth, out var value))
        {
            return true;
        }

        var tag = context.OperationDescription.Operation.Tags.FirstOrDefault();

        securityRequirement[JwtBearerAuth] = tag switch
        {
            var t when string.Equals(t, ServiceOwnerGroup.RoutePrefix, StringComparison.OrdinalIgnoreCase)
                => IsServiceOwnerSearchEndpoint(context.OperationDescription)
                    ? [AuthorizationScope.ServiceProvider, AuthorizationScope.ServiceProviderSearch]
                    : [AuthorizationScope.ServiceProvider],

            var t when string.Equals(t, EndUserGroup.RoutePrefix, StringComparison.OrdinalIgnoreCase)
                => [AuthorizationScope.EndUser],

            _ => value
        };

        if (string.Equals(tag, EndUserGroup.RoutePrefix, StringComparison.OrdinalIgnoreCase))
        {
            context.OperationDescription.Operation.Security!.Add(new OpenApiSecurityRequirement
            {
                [IdPortenSecurityScheme] = [AuthorizationScope.EndUser, AuthorizationScope.EndUserNoConsent]
            });
        }

        return true;
    }

    private static bool IsServiceOwnerSearchEndpoint(OpenApiOperationDescription description) =>
        description.Method == OpenApiOperationMethod.Get
        && ServiceOwnerSearchPaths.Contains(description.Path);
}
