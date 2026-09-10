using System.Reflection;
using System.Text;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Swagger;

public sealed class AuthorizationFailureMessageBuilder
{
    private readonly List<string> _messages;

    private AuthorizationFailureMessageBuilder(params string[] messages)
    {
        if (messages.Length == 0) throw new ArgumentException("Messages cannot be empty.");
        _messages = messages.ToList();
    }

    public AuthorizationFailureMessageBuilder Or(string message)
    {
        _messages.Add(message);
        return this;
    }


    public string Build()
    {
        if (_messages.Count == 1) return _messages[0];
        var sb = new StringBuilder();

        if (_messages.Count > 1) sb.AppendLine("One of: ");

        sb.Append(string.Join("\n", _messages.Select(m => $"- {m}")));

        return sb.ToString();
    }

    public static AuthorizationFailureMessageBuilder DefaultForbiddenFor<TEndpoint>() where TEndpoint : class
    {
        var metadata = typeof(TEndpoint).GetCustomAttribute<OpenApiExtrasAttribute>()
                       ?? throw new InvalidOperationException(
                           $"Endpoint {typeof(TEndpoint).FullName} missing {nameof(OpenApiExtrasAttribute)}"
                       );
        return DefaultForbiddenFor(metadata);
    }

    public static AuthorizationFailureMessageBuilder DefaultForbiddenFor(OpenApiExtrasAttribute metadata)
    {
        var flow = metadata.SecuritySchemes.Select(x => x switch
        {
            OpenApiSecurityScheme.IdportenSecurityScheme => "ID-porten",
            OpenApiSecurityScheme.MaskinportenSecurityScheme => "Maskinporten",
            _ => throw new ArgumentOutOfRangeException($"Unknown security scheme: {x}")
        });

        var requiredFlows = string.Join(" or ", flow);
        var requiredScopes = string.Join(" ", metadata.Scopes);

        return new AuthorizationFailureMessageBuilder(
            Constants.SwaggerSummary
                .AuthorizationFailure
                .FormatInvariant(requiredFlows, requiredScopes)
        );
    }
}
