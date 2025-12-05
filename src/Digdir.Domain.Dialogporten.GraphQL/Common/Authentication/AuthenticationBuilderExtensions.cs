using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common;
using Microsoft.IdentityModel.JsonWebTokens;
using NSec.Cryptography;
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
                    OnAuthenticationFailed = context =>
                    {
                        Debug.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    // OnMessageReceived = async context =>
                    // {
                    //     var expectedIssuer = await context.HttpContext
                    //         .RequestServices
                    //         .GetRequiredService<ITokenIssuerCache>()
                    //         .GetIssuerForScheme(schema.Name);
                    //
                    //     if (!context.HttpContext.Items.TryGetValue(Constants.CurrentTokenIssuer, out var tokenIssuer)
                    //         || (string?)tokenIssuer != expectedIssuer)
                    //     {
                    //         context.NoResult();
                    //     }
                    // }
                };
            });
        }

        var applicationSettings = configuration
            .GetSection(ApplicationSettings.ConfigurationSectionName)
            .Get<ApplicationSettings>();

        var keyPairs = applicationSettings!.Dialogporten.Ed25519KeyPairs;
        _dialogportenIssuer = applicationSettings?.Dialogporten.BaseUri.AbsoluteUri.TrimEnd('/') + DialogTokenIssuerVersion;

        _primaryPublicKey = PublicKey.Import(SignatureAlgorithm.Ed25519,
            Base64Url.Decode(keyPairs.Primary.PublicComponent), KeyBlobFormat.RawPublicKey);

        _secondaryPublicKey = PublicKey.Import(SignatureAlgorithm.Ed25519,
            Base64Url.Decode(keyPairs.Secondary.PublicComponent), KeyBlobFormat.RawPublicKey);

        authenticationBuilder.AddJwtBearer(DialogportenAuthenticationSchemaName, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                RequireSignedTokens = false,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = true,
                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromSeconds(2),
                // IssuerSigningKeys = issuerSigningKeys
                SignatureValidator = ValidateSignature
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Debug.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },

                OnMessageReceived = context =>
                {
                    if (context.HttpContext.Items.TryGetValue(Constants.CurrentTokenIssuer, out var tokenIssuer)
                    && (string?)tokenIssuer != _dialogportenIssuer)
                    {
                        context.NoResult();
                    }

                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    private static PublicKey? _primaryPublicKey;
    private static PublicKey? _secondaryPublicKey;
    private static string? _dialogportenIssuer;

    private static JsonWebToken ValidateSignature(string encodedToken, object _)
    {
        var handler = new JsonWebTokenHandler();
        var jwt = handler.ReadJsonWebToken(encodedToken);

        var signature = Base64Url.Decode(jwt.EncodedSignature);
        var signatureIsValid = SignatureAlgorithm.Ed25519
            .Verify(_primaryPublicKey!, Encoding.UTF8.GetBytes(jwt.EncodedHeader + '.' + jwt.EncodedPayload), signature);

        if (!signatureIsValid)
        {
            signatureIsValid = SignatureAlgorithm.Ed25519
                .Verify(_secondaryPublicKey!, Encoding.UTF8.GetBytes(jwt.EncodedHeader + '.' + jwt.EncodedPayload), signature);
        }

        if (signatureIsValid)
        {
            return jwt;
        }

        throw new SecurityTokenInvalidSignatureException("Invalid token signature.");
    }
}
