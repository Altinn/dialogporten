using System.Reflection;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Swagger;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class OpenApiExtrasAttribute : Attribute
{

    /// <summary>
    /// The publicly known scopes this endpoint requires.
    ///
    /// </summary>
    public string[] Scopes { get; }

    /// <summary>
    /// The security schemes this endpoint supports
    /// Must be one of: <see cref="OpenApiSecurityScheme"/>
    /// </summary>
    public string[] SecuritySchemes { get; }

    public OpenApiExtrasAttribute(string[] scopes, string[] securitySchemes)
    {
        if (scopes is null or { Length: 0 })
        {
            throw new ArgumentException("Scopes must not be null or empty.", nameof(scopes));
        }

        if (securitySchemes is null or { Length: 0 })
        {
            throw new ArgumentException("SecuritySchemes must not be null or empty.", nameof(securitySchemes));
        }

        Scopes = scopes;
        SecuritySchemes = securitySchemes;
    }

    public static string Get401Error<TEndpoint>() where TEndpoint : class
    {
        var metadata = typeof(TEndpoint).GetCustomAttribute<OpenApiExtrasAttribute>()
                       ?? throw new InvalidOperationException(
                           $"Endpoint {typeof(TEndpoint).FullName} missing {nameof(OpenApiExtrasAttribute)}"
                       );
        var flow = metadata.SecuritySchemes.Select(x => x switch
        {
            OpenApiSecurityScheme.IdportenSecurityScheme => "ID-porten",
            OpenApiSecurityScheme.MaskinportenSecurityScheme => "Maskinporten",
            _ => throw new ArgumentOutOfRangeException($"Unknown security scheme: {x}")
        });

        var requiredFlows = string.Join(" or ", flow);
        var requiredScopes = string.Join(" ", metadata.Scopes);

        return Constants.SwaggerSummary
            .AuthenticationFailure
            .FormatInvariant(requiredFlows, requiredScopes);
    }
}
