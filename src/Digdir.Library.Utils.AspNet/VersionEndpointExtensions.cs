using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Library.Utils.AspNet;

public static class VersionEndpointExtensions
{
    /// <summary>
    /// Holds the image tag. The Dockerfiles set it from the VERSION build argument; local builds leave it unset.
    /// </summary>
    public const string VersionVariable = "DIALOGPORTEN_VERSION";

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps an anonymous GET endpoint that returns the image tag as plain text, so a deploy can wait until the
        /// new version answers before it runs tests against it. It answers 404 when the version is unset. The
        /// endpoint is left out of the OpenAPI documents, and its responses are marked no-store so no cache can
        /// give a version check an old answer.
        /// </summary>
        /// <param name="pattern">The route, under a path prefix that the edge forwards to this app.</param>
        public RouteHandlerBuilder MapVersionEndpoint(string pattern)
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

            var version = endpoints.ServiceProvider.GetRequiredService<IConfiguration>()[VersionVariable];

            return endpoints
                .MapGet(pattern, (HttpContext context) =>
                {
                    context.Response.Headers.CacheControl = "no-store";
                    return string.IsNullOrWhiteSpace(version)
                        ? Results.NotFound()
                        : Results.Text(version);
                })
                .AllowAnonymous()
                .ExcludeFromDescription();
        }
    }
}
