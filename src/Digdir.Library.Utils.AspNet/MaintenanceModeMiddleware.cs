using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Digdir.Library.Utils.AspNet;

public class MaintenanceModeMiddleware
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

        if (settings.MaintenanceMode.Enabled)
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
}

public static class MaintenanceModeMiddlewareExtensions
{
    public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder app)
        => app.UseMiddleware<MaintenanceModeMiddleware>();
}
