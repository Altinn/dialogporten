using Digdir.Domain.Dialogporten.Application.Common.Context;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Middleware to automatically capture cost management metrics
/// </summary>
public sealed class CostManagementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICostManagementMetricsService _metricsService;
    private readonly ILogger<CostManagementMiddleware> _logger;

    public CostManagementMiddleware(
        RequestDelegate next,
        ICostManagementMetricsService metricsService,
        ILogger<CostManagementMiddleware> logger)
    {
        _next = next;
        _metricsService = metricsService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? tokenOrg = null;

        // Determine transaction type from endpoint metadata (attribute-based)
        var transactionType = ResolveTransactionTypeFromEndpointMetadata(context);

        // If we can't map to a transaction type, skip metrics and just process the request
        if (!transactionType.HasValue)
        {
            await _next(context);
            return;
        }

        // Extract organization short name from authenticated user claims
        try
        {
            tokenOrg = ExtractTokenOrg(context);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract organization short name from user claims for {Method} {Path}",
                context.Request.Method, context.Request.Path.Value);
        }

        // Process the request
        await _next(context);

        // Post-processing: Record metrics and logging in a try-catch to isolate failures
        try
        {
            // Resolve IApplicationContext from the request scope
            var applicationContext = context.RequestServices.GetService<IApplicationContext>();

            // Safely read metadata from application context
            var serviceOrg = applicationContext?.Metadata.TryGetValue("serviceOrg", out var orgValue) == true ? orgValue : null;
            var serviceResource = applicationContext?.Metadata.TryGetValue("serviceResource", out var srValue) == true ? srValue : null;

            // Log warning if metadata is missing but continue gracefully
            if (applicationContext == null)
            {
                _logger.LogWarning("IApplicationContext not available for cost management metrics on {Method} {Path}",
                    context.Request.Method, context.Request.Path.Value);
            }
            else if (serviceOrg == CostManagementConstants.UnknownValue || serviceResource == CostManagementConstants.UnknownValue)
            {
                _logger.LogWarning("Unknown organization/resource for cost management on {Method} {Path}: serviceOrg={ServiceOrg}, serviceResource={ServiceResource}",
                    context.Request.Method, context.Request.Path.Value, serviceOrg, serviceResource);
            }

            // Log both organization identifiers for debugging
            _logger.LogDebug("Cost management metadata for {Method} {Path}: TokenOrg={TokenOrg}, ServiceOrg={ServiceOrg}, ServiceResource={ServiceResource}",
                context.Request.Method, context.Request.Path.Value, tokenOrg, serviceOrg, serviceResource);

            // Record the metric after successful processing
            _metricsService.RecordTransaction(
                transactionType.Value,
                context.Response.StatusCode,
                tokenOrg,
                serviceOrg,
                serviceResource);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to capture cost management metrics for {Method} {Path}. TransactionType: {TransactionType}, TokenOrg: {TokenOrg}",
                context.Request.Method,
                context.Request.Path.Value,
                transactionType?.ToString() ?? "Unknown",
                tokenOrg ?? "null");
        }
    }

    private static TransactionType? ResolveTransactionTypeFromEndpointMetadata(HttpContext context)
    {
        // Try to get the endpoint metadata added by FastEndpoints configurator
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            return null;
        }

        var endpointTypeMetadata = endpoint.Metadata.GetMetadata<EndpointTypeMetadata>();
        if (endpointTypeMetadata?.EndpointType is null)
        {
            return null;
        }

        var costTrackedAttr = endpointTypeMetadata.EndpointType
            .GetCustomAttributes(typeof(CostTrackedAttribute), inherit: false)
            .OfType<CostTrackedAttribute>()
            .FirstOrDefault();

        if (costTrackedAttr is null)
        {
            return null;
        }

        // Check if this endpoint has a query parameter variant
        if (costTrackedAttr.HasVariant &&
            costTrackedAttr.QueryParameterVariant != null &&
            context.Request.Query.ContainsKey(costTrackedAttr.QueryParameterVariant))
        {
            return costTrackedAttr.VariantTransactionType;
        }

        return costTrackedAttr.TransactionType;
    }

    private static string? ExtractTokenOrg(HttpContext context)
    {
        var user = context.RequestServices.GetRequiredService<IUser>();
        var principal = user.GetPrincipal();

        if (principal.TryGetOrganizationShortName(out var orgShortName))
        {
            return orgShortName;
        }

        return null;
    }
}

/// <summary>
/// Extension methods for registering cost management middleware
/// </summary>
public static class CostManagementMiddlewareExtensions
{
    /// <summary>
    /// Adds cost management middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseCostManagementMetrics(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CostManagementMiddleware>();
    }

    /// <summary>
    /// Registers cost management services
    /// </summary>
    public static IServiceCollection AddCostManagementMetrics(this IServiceCollection services)
    {
        services.AddSingleton<ICostManagementMetricsService, CostManagementMetricsService>();

        return services;
    }
}
