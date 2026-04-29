using Digdir.Library.Utils.AspNet.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics.CodeAnalysis;

namespace Digdir.Library.Utils.AspNet;

public static class AspNetUtilitiesExtensions
{
    public static IServiceCollection AddAspNetHealthChecks(this IServiceCollection services, Action<AspNetUtilitiesSettings, IServiceProvider>? configure = null)
    {
        var optionsBuilder = services.AddOptions<AspNetUtilitiesSettings>();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        return services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["self"])
            .AddCheck<EndpointsHealthCheck>(
                "Endpoints",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["external"])
            .Services;
    }

    public static WebApplication MapAspNetHealthChecks(this WebApplication app) =>
        app.MapHealthCheckEndpoint("/health/startup", check => check.Tags.Contains("dependencies"))
            .MapHealthCheckEndpoint("/health/liveness", check => check.Tags.Contains("self"))
            .MapHealthCheckEndpoint("/health/readiness", check => check.Tags.Contains("critical"))
            .MapHealthCheckEndpoint("/health", check => check.Tags.Contains("dependencies"))
            .MapHealthCheckEndpoint("/health/deep", check => check.Tags.Contains("dependencies") || check.Tags.Contains("external"));

    private static WebApplication MapHealthCheckEndpoint(this WebApplication app, string path, Func<HealthCheckRegistration, bool> predicate)
    {
        app.MapHealthChecks(path, new HealthCheckOptions { Predicate = predicate, ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
        return app;
    }

    public static List<HttpGetEndpointToCheck> ResolveHttpGetEndpointsToCheck(
        IEnumerable<HttpGetEndpointToCheck> endpoints,
        Uri altinnBaseUri,
        IEnumerable<HttpGetEndpointToCheck>? additionalEndpoints = null) =>
        [
            ..endpoints.Select(endpoint => ResolveHttpGetEndpointToCheck(endpoint, altinnBaseUri)),
            ..(additionalEndpoints ?? []).Select(endpoint => ResolveHttpGetEndpointToCheck(endpoint, altinnBaseUri))
        ];

    public static HttpGetEndpointToCheck ResolveHttpGetEndpointToCheck(HttpGetEndpointToCheck endpoint, Uri altinnBaseUri)
    {
        if (HasAbsoluteUrl(endpoint, out var url))
        {
            return endpoint with
            {
                Url = url,
                AltinnPlatformRelativePath = null
            };
        }

        return endpoint with
        {
            Url = new Uri(altinnBaseUri, endpoint.AltinnPlatformRelativePath).AbsoluteUri,
            AltinnPlatformRelativePath = null
        };
    }

    private static bool HasAbsoluteUrl(HttpGetEndpointToCheck endpoint, [NotNullWhen(true)] out string? url)
    {
        url = endpoint.Url;
        return !string.IsNullOrWhiteSpace(url);
    }
}
