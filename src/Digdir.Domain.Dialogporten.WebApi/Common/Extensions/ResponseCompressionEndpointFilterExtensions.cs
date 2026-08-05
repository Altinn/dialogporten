using System.Reflection;
using Digdir.Library.Utils.AspNet;
using Microsoft.AspNetCore.Http.Features;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

internal static class ResponseCompressionEndpointFilterExtensions
{
    /// <summary>
    /// If any public instance method on <paramref name="endpointType"/> carries
    /// <see cref="EnableResponseCompressionAttribute"/>, attaches an endpoint filter that opts the
    /// response in to compression over HTTPS by setting <see cref="IHttpsCompressionFeature.Mode"/>.
    /// </summary>
    public static RouteHandlerBuilder AddResponseCompressionHintIfMarked(
        this RouteHandlerBuilder routeHandlerBuilder, Type endpointType)
    {
        var marked = endpointType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Any(m => m.GetCustomAttribute<EnableResponseCompressionAttribute>() is not null);

        if (!marked)
        {
            return routeHandlerBuilder;
        }

        routeHandlerBuilder.AddEndpointFilter((ctx, next) =>
        {
            if (ctx.HttpContext.Features.Get<IHttpsCompressionFeature>() is { } feature)
            {
                feature.Mode = HttpsCompressionMode.Compress;
            }
            return next(ctx);
        });
        return routeHandlerBuilder;
    }
}
