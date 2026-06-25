using Azure;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using static Digdir.Domain.Dialogporten.WebApi.Common.Extensions.SecurityRequirementExtensions;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Authorization;

internal sealed class AuthorizationOptionsSetup : IConfigureOptions<AuthorizationOptions>
{
    private readonly WebApiSettings _options;

    public AuthorizationOptionsSetup(IOptions<WebApiSettings> options)
    {
        _options = options.Value;
    }

    internal static readonly Dictionary<string, (ScopeRequirementOperation Operation, string[] Scopes)[]> ScopeRulesByPolicy = new()
    {
        [AuthorizationPolicy.EndUser] =
        [
            (ScopeRequirementOperation.And, [AuthorizationScope.EndUser])
        ],
        [AuthorizationPolicy.ServiceProvider] =
        [
            (ScopeRequirementOperation.And, [AuthorizationScope.ServiceProvider])
        ],
        [AuthorizationPolicy.ServiceProviderSearch] =
        [
            (
                ScopeRequirementOperation.And,
                [
                    AuthorizationScope.ServiceProvider,
                    AuthorizationScope.ServiceProviderSearch
                ]
            )
        ],
        [AuthorizationPolicy.NotificationConditionCheck] =
        [
            (ScopeRequirementOperation.And, [AuthorizationScope.NotificationConditionCheck])
        ],
        [AuthorizationPolicy.ServiceProviderAdmin] =
        [
            (
                ScopeRequirementOperation.And,
                [
                    AuthorizationScope.ServiceProvider,
                    AuthorizationScope.ServiceOwnerAdminScope
                ]
            )
        ],
        [AuthorizationPolicy.Testing] =
        [
            (ScopeRequirementOperation.And, [AuthorizationScope.Testing])
        ],
    };

    public void Configure(AuthorizationOptions options)
    {
        var authenticationSchemas = _options
            .Authentication
            .JwtBearerTokenSchemas
            .Select(x => x.Name)
            .ToArray();

        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(authenticationSchemas)
            .Build();

        options.AddPolicy(AuthorizationPolicy.EndUser, builder => builder
            .Combine(options.DefaultPolicy)
            .RequireAssertion(context =>
            {
                var userScopes = context.User
                    .FindAll(AuthorizationPolicyBuilderExtensions.ScopeClaim)
                    .SelectMany(s => s.Value.Split(" "))
                    .ToList();

                return userScopes.Contains(AuthorizationScope.EndUser) ||
                       userScopes.Contains(AuthorizationScope.EndUserNoConsent);
            }));

        options.AddPolicy(AuthorizationPolicy.ServiceProvider, builder => builder
            .Combine(options.DefaultPolicy)
            .RequireValidConsumerClaim()
            .RequireScope(AuthorizationScope.ServiceProvider));

        options.AddPolicy(AuthorizationPolicy.ServiceProviderSearch, builder => builder
            .Combine(options.GetPolicy(AuthorizationPolicy.ServiceProvider)!)
            .RequireScope(AuthorizationScope.ServiceProviderSearch));

        options.AddPolicy(AuthorizationPolicy.Testing, builder => builder
            .Combine(options.DefaultPolicy)
            .RequireScope(AuthorizationScope.Testing));

        options.AddPolicy(AuthorizationPolicy.NotificationConditionCheck, builder => builder
            .Combine(options.DefaultPolicy)
            .RequireValidConsumerClaim()
            .RequireScope(AuthorizationScope.NotificationConditionCheck));

        options.AddPolicy(AuthorizationPolicy.ServiceProviderAdmin, builder => builder
            .Combine(options.GetPolicy(AuthorizationPolicy.ServiceProvider)!)
            .RequireScope(AuthorizationScope.ServiceOwnerAdminScope)
        );
    }
}
