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
        // TODO: Analyze token to extract user and org info for feature metrics - do not log SSNs
        var deliveryContext = context.RequestServices.GetRequiredService<IFeatureMetricDeliveryContext>();
        try
        {
            await _next(context);
        }
        catch (Exception)
        {
            deliveryContext.Ack(GeneratePresentationTag(context),
                new("StatusCode", "5**"),
                new("CorrelationId", context.TraceIdentifier),
                new("Status", "error"));
            throw;
        }

        deliveryContext.Ack(GeneratePresentationTag(context),
            new("StatusCode", context.Response.StatusCode),
            new("CorrelationId", context.TraceIdentifier),
            new("Status", IsSuccessStatusCode(context.Response.StatusCode) ? "success" : "failure"));
    }

    private static bool IsSuccessStatusCode(int statusCode) => statusCode is >= 200 and < 300;

    private static string GeneratePresentationTag(HttpContext context) =>
        // Generate a presentation tag containing relevant context information
        // TODO: need raw route path here, not the processed one
        // E.g., GET_api_v1_messages_{messageId}_attachments_{attachmentId}
        // instead of GET_api_v1_messages_123_attachments_456
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
