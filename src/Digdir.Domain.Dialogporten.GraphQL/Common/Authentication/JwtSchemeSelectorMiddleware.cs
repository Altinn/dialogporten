using System.IdentityModel.Tokens.Jwt;

namespace Digdir.Domain.Dialogporten.GraphQL.Common.Authentication;

public sealed class JwtSchemeSelectorMiddleware
{
    private readonly RequestDelegate _next;

    public JwtSchemeSelectorMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(Constants.Authorization, out var authorizationHeader))
            return _next(context);

        var token = authorizationHeader.ToString()
            .Split(' ')
            .LastOrDefault();

        if (string.IsNullOrEmpty(token))
            return _next(context);

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
            return _next(context);

        try
        {
            var jwtToken = handler.ReadJwtToken(token);
            context.Items[Constants.CurrentTokenIssuer] = jwtToken.Issuer;
        }
        catch (ArgumentException) { }

        return _next(context);
    }
}

public static class JwtSchemeSelectorMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtSchemeSelector(this IApplicationBuilder app)
        => app.UseMiddleware<JwtSchemeSelectorMiddleware>();
}
