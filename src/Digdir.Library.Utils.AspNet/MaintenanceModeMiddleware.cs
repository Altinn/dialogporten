using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Digdir.Library.Utils.AspNet;

internal sealed class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceModeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var settings = context.RequestServices
            .GetRequiredService<IOptionsSnapshot<WebHostCommonSettings>>()
            .Value;

        if (settings.MaintenanceMode.Enabled && !IsAllowedDuringMaintenance(context))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/plain";

            if (settings.MaintenanceMode.RetryAt.HasValue)
            {
                context.Response.Headers["Retry-After"] = settings.MaintenanceMode.RetryAt.Value
                    .ToUniversalTime()
                    .ToString("R"); // RFC1123 format
            }
            await context.Response.WriteAsync("Service Unavailable");
            return;
        }

        await _next(context);
    }

    // Endpoint metadata is only there once routing has matched the request. Neither host calls
    // UseRouting explicitly, so WebApplication routes before any app.Use* middleware runs.
    private static bool IsAllowedDuringMaintenance(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<AllowDuringMaintenanceAttribute>() is not null;
}

public static class MaintenanceModeMiddlewareExtensions
{
    /// <summary>
    /// Answers every request with 503 while maintenance mode is on, except requests to endpoints
    /// marked with <see cref="AllowDuringMaintenanceAttribute"/>.
    /// </summary>
    public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder app)
        => app.UseMiddleware<MaintenanceModeMiddleware>();

    extension<TBuilder>(TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        /// <summary>
        /// Lets requests to the endpoint through while maintenance mode is on.
        /// </summary>
        public TBuilder AllowDuringMaintenance() =>
            builder.WithMetadata(new AllowDuringMaintenanceAttribute());
    }
}
