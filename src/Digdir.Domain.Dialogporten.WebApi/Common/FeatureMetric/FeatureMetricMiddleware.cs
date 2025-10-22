using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.WebApi.Common.FeatureMetric;

/// <summary>
/// Middleware to handle feature metric delivery acknowledgments based on HTTP response status codes
/// </summary>
public sealed class FeatureMetricMiddleware(RequestDelegate next, IOptions<FeatureMetricOptions> options)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly FeatureMetricOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip feature metric tracking for health check endpoints
        if (ShouldSkipFeatureMetrics(context))
        {
            await _next(context);
            return;
        }

        // TODO: Analyze token to extract user and org info for feature metrics - do not log SSNs
        var deliveryContext = context.RequestServices.GetRequiredService<IFeatureMetricDeliveryContext>();
        try
        {
            await _next(context);
        }
        catch (Exception)
        {
            deliveryContext.ReportOutcome(GeneratePresentationTag(context),
                new("StatusCode", "5**"),
                new("CorrelationId", context.TraceIdentifier),
                new("Status", "error"));
            throw;
        }

        deliveryContext.ReportOutcome(GeneratePresentationTag(context),
            new("StatusCode", context.Response.StatusCode),
            new("CorrelationId", context.TraceIdentifier),
            new("Status", IsSuccessStatusCode(context.Response.StatusCode) ? "success" : "failure"));
    }

    private static bool IsSuccessStatusCode(int statusCode) => statusCode is >= 200 and < 300;

    private bool ShouldSkipFeatureMetrics(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return _options.ExcludedPathPrefixes.Any(excludedPath =>
            path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
    }

    private static string GeneratePresentationTag(HttpContext context)
    {
        var method = context.Request.Method;
        var endpoint = context.GetEndpoint();

        var queryParam = GetQueryParams(context);

        // Get the route template from endpoint metadata
        var template = GetRouteTemplate(endpoint);
        if (!string.IsNullOrEmpty(template))
        {
            return $"{method}_{template.Trim('/')}{queryParam}";
        }

        // Fallback: Use actual path if template not available
        var path = context.Request.Path.Value?.Trim('/') ?? "";
        return $"{method}_{path}{queryParam}";
    }

    private static string GetQueryParams(HttpContext context)
    {
        var queryParam = string.Empty;

        if (context.Request.Query.ContainsKey(nameof(SearchDialogQuery.EndUserId)))
        {
            queryParam += $"_{nameof(SearchDialogQuery.EndUserId)}";
        }

        return queryParam;
    }

    private static string? GetRouteTemplate(Endpoint? endpoint)
    {
        if (endpoint == null) return null;

        // Try RouteEndpoint first
        if (endpoint is RouteEndpoint routeEndpoint)
        {
            return routeEndpoint.RoutePattern.RawText;
        }

        // Try to get route template from endpoint display name
        var displayName = endpoint.DisplayName;
        if (!string.IsNullOrEmpty(displayName) && displayName.Contains(' '))
        {
            // Display name format is usually "HTTP method route" (e.g., "GET api/v1/dialogs/{dialogId}")
            var parts = displayName.Split(' ', 2);
            if (parts.Length == 2)
            {
                return parts[1];
            }
        }

        return null;
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
