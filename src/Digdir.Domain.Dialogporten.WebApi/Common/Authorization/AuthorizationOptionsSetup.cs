﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Authorization;

internal sealed class AuthorizationOptionsSetup : IConfigureOptions<AuthorizationOptions>
{
    private readonly WebApiSettings _options;

    public AuthorizationOptionsSetup(IOptions<WebApiSettings> options)
    {
        _options = options.Value;
    }

    public void Configure(AuthorizationOptions options)
    {
        var authenticatonSchemas = _options
            .Authentication
            .JwtBearerTokenSchemas
            .Select(x => x.Name)
            .ToArray();

        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(authenticatonSchemas)
            .RequireValidConsumerClaim()
            .Build();

        options.AddPolicy(AuthorizationPolicy.EndUser, builder => builder
            .Combine(options.DefaultPolicy)
            .RequireScope(AuthorizationScope.EndUser));

        options.AddPolicy(AuthorizationPolicy.ServiceProvider, builder => builder
            .Combine(options.DefaultPolicy)
            .RequireScope(AuthorizationScope.ServiceProvider));

        options.AddPolicy(AuthorizationPolicy.ServiceProviderSearch, builder => builder
            .Combine(options.DefaultPolicy)
            .RequireScope(AuthorizationScope.ServiceProviderSearch));

        options.AddPolicy(AuthorizationPolicy.Testing, builder => builder
            .Combine(options.DefaultPolicy)
            .RequireScope(AuthorizationScope.Testing));
    }
}
