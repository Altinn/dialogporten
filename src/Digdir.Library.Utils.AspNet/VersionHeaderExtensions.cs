using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Library.Utils.AspNet;

public static class VersionHeaderExtensions
{
    /// <summary>
    /// Response header with the version of the running image, so a deploy can wait until the new version
    /// answers before it runs tests against it.
    /// </summary>
    public const string HeaderName = "X-Dialogporten-Version";

    /// <summary>
    /// Request header that asks for <see cref="HeaderName"/>. Responses to other requests do not carry the
    /// version. A version check should also send Cache-Control: no-cache and use a path that is not publicly
    /// cacheable, since a cache that holds an ordinary response can answer without reaching the app.
    /// </summary>
    public const string RequestHeaderName = "X-Dialogporten-Version-Check";

    /// <summary>
    /// Holds the image tag. The Dockerfiles set it from the VERSION build argument; local builds leave it
    /// unset, and then no header is sent.
    /// </summary>
    public const string VersionVariable = "DIALOGPORTEN_VERSION";

    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Adds <see cref="HeaderName"/> to every response the app writes for a request that carries
        /// <see cref="RequestHeaderName"/>, including challenge, not-found and handled error responses. A request
        /// that fails with an exception no middleware handles gets Kestrel's own 500, which has no header.
        /// Those responses are also marked no-store, so no cache can hand the version to other callers or an old
        /// answer to a version check. Register it before any other middleware, so it is in place before a
        /// response can start, and so its callback runs after any that set caching headers.
        /// </summary>
        public IApplicationBuilder UseVersionHeader()
        {
            var version = app.ApplicationServices.GetRequiredService<IConfiguration>()[VersionVariable];
            if (string.IsNullOrWhiteSpace(version))
            {
                return app;
            }

            return app.Use((context, next) =>
            {
                if (!context.Request.Headers.ContainsKey(RequestHeaderName))
                {
                    return next(context);
                }

                context.Response.OnStarting(() =>
                {
                    context.Response.Headers[HeaderName] = version;
                    context.Response.Headers.CacheControl = "no-store";
                    return Task.CompletedTask;
                });

                return next(context);
            });
        }
    }
}
