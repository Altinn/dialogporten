using System.Drawing;
using System.Globalization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

namespace Digdir.Domain.Dialogporten.WebApi.Common.FeatureMetric;

/// <summary>
/// Middleware to handle feature metric delivery acknowledgments based on HTTP response status codes
/// </summary>
public sealed class FeatureMetricMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    public async Task InvokeAsync(HttpContext context)
    {
        var deliveryContext = context.RequestServices.GetRequiredService<IFeatureMetricDeliveryContext>();
        await _next(context);
        // TODO: what about server errors? Should we ignore them?
        var presentationTag = GeneratePresentationTag(context);
        if (!IsSuccessStatusCode(context.Response.StatusCode))
        {
            deliveryContext.Nack(presentationTag,
                new("StatusCode", context.Response.StatusCode.ToString(CultureInfo.InvariantCulture)),
                new("CorrelationId", context.TraceIdentifier));
            return;
        }

        deliveryContext.Ack(presentationTag,
            new("StatusCode", context.Response.StatusCode.ToString(CultureInfo.InvariantCulture)),
            new("CorrelationId", context.TraceIdentifier));
    }

    private static bool IsSuccessStatusCode(int statusCode) => statusCode is >= 200 and < 300;

    private static string GeneratePresentationTag(HttpContext context) =>
        // Generate a presentation tag containing relevant context information
        // TODO: need raw route path here, not the processed one
        $"{context.Request.Method}_{context.Request.Path.Value?.Replace("/", "_").Trim('_')}";
}

/// <summary>
/// Extension methods for registering feature metric middleware
/// </summary>
public static class FeatureMetricMiddlewareExtensions
{
    /// <summary>
    /// Adds feature metric middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseFeatureMetrics(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FeatureMetricMiddleware>();
    }
}
