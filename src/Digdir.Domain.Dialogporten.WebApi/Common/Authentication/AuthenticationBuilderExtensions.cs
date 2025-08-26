using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Authentication;

internal static class AuthenticationBuilderExtensions
{
    public static IServiceCollection AddDialogportenAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtTokenSchemas = configuration
            .GetSection(WebApiSettings.SectionName)
            .Get<WebApiSettings>()
            ?.Authentication
            ?.JwtBearerTokenSchemas;

        if (jwtTokenSchemas is null || jwtTokenSchemas.Count == 0)
        {
            // Validation should have caught this.
            throw new UnreachableException();
        }

        services.AddSingleton<ITokenIssuerCache, TokenIssuerCache>();

        // Turn off mapping InboundClaims names to its longer version
        // "acr" => "http://schemas.microsoft.com/claims/authnclassreference"
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        var authenticationBuilder = services.AddAuthentication();

        foreach (var schema in jwtTokenSchemas)
        {
            authenticationBuilder.AddJwtBearer(schema.Name, options =>
            {
                options.MetadataAddress = schema.WellKnown;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(2)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = async context =>
                    {
                        var expectedIssuer = await context.HttpContext
                            .RequestServices
                            .GetRequiredService<ITokenIssuerCache>()
                            .GetIssuerForScheme(schema.Name);

                        if (context.HttpContext.Items.TryGetValue(Constants.CurrentTokenIssuer, out var tokenIssuer)
                            && (string?)tokenIssuer != expectedIssuer)
                        {
                            context.NoResult();
                        }
                    }
                };
            });
        }

        return services;
    }
}
