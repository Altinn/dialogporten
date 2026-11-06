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
    /// Holds the image tag. The Dockerfiles set it from the VERSION build argument; local builds leave it
    /// unset, and then no header is sent.
    /// </summary>
    public const string VersionVariable = "DIALOGPORTEN_VERSION";

    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Adds <see cref="HeaderName"/> to every response, including error and challenge responses.
        /// Register it before any other middleware, so it is in place before a response can start.
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
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers[HeaderName] = version;
                    return Task.CompletedTask;
                });

                return next(context);
            });
        }
    }
}
