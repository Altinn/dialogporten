using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ScottBrady.IdentityModel.Crypto;
using ScottBrady.IdentityModel.Tokens;
using static Digdir.Domain.Dialogporten.Application.Features.V1.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.GraphQL.Common.Authentication;

internal static class AuthenticationBuilderExtensions
{
    internal const string DialogportenAuthenticationSchemaName = "Dialogporten";

    public static IServiceCollection AddDialogportenAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtTokenSchemas = configuration
            .GetSection(GraphQlSettings.SectionName)
            .Get<GraphQlSettings>()?
            .Authentication?
            .JwtBearerTokenSchemas;

        if (jwtTokenSchemas is null || jwtTokenSchemas.Count == 0)
            // Validation should have caught this.
            throw new UnreachableException();

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

        authenticationBuilder.AddJwtBearer(DialogportenAuthenticationSchemaName, options =>
        {
            var (dialogportenIssuer, primaryKey, secondaryKey) = GetAuthConfig(configuration);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(2),
                IssuerSigningKeys = [primaryKey, secondaryKey]
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.HttpContext.Items.TryGetValue(Constants.CurrentTokenIssuer, out var tokenIssuer)
                        && (string?)tokenIssuer != dialogportenIssuer)
                    {
                        context.NoResult();
                    }

                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    private static (string issuer, EdDsaSecurityKey primaryKey, EdDsaSecurityKey secondaryKey) GetAuthConfig(IConfiguration configuration)
    {
        var applicationSettings = configuration
            .GetSection(ApplicationSettings.ConfigurationSectionName)
            .Get<ApplicationSettings>() ?? throw new InvalidOperationException(
            $"Missing config '{ApplicationSettings.ConfigurationSectionName}'.");

        var issuer = applicationSettings.Dialogporten.BaseUri.AbsoluteUri.TrimEnd('/') + DialogTokenIssuerVersion;

        var keyPairs = applicationSettings.Dialogporten.Ed25519KeyPairs;

        return (issuer, CreateKey(keyPairs.Primary), CreateKey(keyPairs.Secondary));
    }

    private static EdDsaSecurityKey CreateKey(Ed25519KeyPair keyPair) =>
        new(EdDsa.Create(new EdDsaParameters(ExtendedSecurityAlgorithms.Curves.Ed25519)
        {
            X = Base64Url.Decode(keyPair.PublicComponent)
        }));
}
