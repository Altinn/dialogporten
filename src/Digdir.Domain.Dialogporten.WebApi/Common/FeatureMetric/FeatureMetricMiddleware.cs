using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

namespace Digdir.Domain.Dialogporten.WebApi.Common.FeatureMetric;

/// <summary>
/// Middleware to handle feature metric delivery acknowledgments based on HTTP response status codes
/// </summary>
public sealed class FeatureMetricMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FeatureMetricMiddleware> _logger;

    public FeatureMetricMiddleware(
        RequestDelegate next,
        ILogger<FeatureMetricMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Process the request first
        await _next(context);

        // After processing, handle feature metric acknowledgment
        try
        {
            var deliveryContext = context.RequestServices.GetService<IFeatureMetricDeliveryContext>();
            // NOTE: This violates clean architecture by accessing the concrete FeatureMetricRecorder class
            // from the presentation layer. This is a pragmatic solution due to middleware execution timing
            // constraints - middleware runs before MediatR behaviors, so we need direct access to update
            // the same instance that the behavior will populate. A proper interface-based solution might be
            // possible, but I've not found a way yet.
            var recorder = context.RequestServices.GetService<FeatureMetricRecorder>();

            if (deliveryContext == null || recorder == null)
            {
                return;
            }

            var statusCode = context.Response.StatusCode;
            var presentationTag = GeneratePresentationTag(context);
            var correlationId = context.TraceIdentifier;

            // Update all recorded metrics with HTTP status code, presentation tag, and correlation ID
            recorder.UpdateRecord(statusCode, presentationTag, correlationId);

            // Determine success/failure based on HTTP status code
            if (IsSuccessStatusCode(statusCode))
            {
                deliveryContext.Ack(presentationTag);
            }
            else
            {
                deliveryContext.Nack(presentationTag);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to acknowledge feature metrics for {Method} {Path}. Status: {StatusCode}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode);
        }
    }

    private static bool IsSuccessStatusCode(int statusCode) => statusCode is >= 200 and < 300;

    private static string GeneratePresentationTag(HttpContext context)
    {
        // Generate a presentation tag containing relevant context information
        return $"{context.Request.Method}_{context.Request.Path.Value?.Replace("/", "_")?.Trim('_')}";
    }
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