using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.GraphQL.Common.Extensions.HotChocolate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using AuthorizationOptions = Microsoft.AspNetCore.Authorization.AuthorizationOptions;

namespace Digdir.Domain.Dialogporten.GraphQL.Common.Authorization;

internal sealed class AuthorizationOptionsSetup : IConfigureOptions<AuthorizationOptions>
{
    private readonly GraphQlSettings _options;

    public AuthorizationOptionsSetup(IOptions<GraphQlSettings> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

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

        options.AddPolicy(AuthorizationPolicy.EndUserSubscription, policy => policy
            .Combine(options.GetPolicy(AuthorizationPolicy.EndUser)!)
            .RequireAssertion(context =>
                context.TryGetDialogEventsSubscriptionDialogId(out var dialogIdTopic)
                && context.User.TryGetClaimValue(DialogTokenClaimTypes.DialogId, out var dialogIdClaimValue)
                && Guid.TryParse(dialogIdClaimValue, out var dialogIdClaim)
                && dialogIdTopic == dialogIdClaim));
    }
}
