using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Digdir.Library.Utils.AspNet;

public class PresentationLayerMaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<AspNetSettings> _settings;

    public PresentationLayerMaintenanceMiddleware(RequestDelegate next, IOptionsMonitor<AspNetSettings> settings)
    {
        _next = next;
        _settings = settings;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var settings = _settings.CurrentValue;
        if (settings.FeatureToggle.PresentationLayerMaintenanceMode)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Service Unavailable");
            return;
        }

        await _next(context);
    }
}

public static class PresentationLayerMaintenanceMiddlewareExtensions
{
    public static IApplicationBuilder UsePresentationLayerMaintenance(this IApplicationBuilder app)
        => app.UseMiddleware<PresentationLayerMaintenanceMiddleware>();
}
