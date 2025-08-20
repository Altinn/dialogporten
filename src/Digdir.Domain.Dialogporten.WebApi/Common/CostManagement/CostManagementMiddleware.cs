namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Middleware to automatically capture cost management metrics
/// </summary>
public sealed class CostManagementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICostManagementMetricsService _metricsService;
    private readonly IServiceIdentifierExtractor _serviceExtractor;
    private readonly ILogger<CostManagementMiddleware> _logger;

    public CostManagementMiddleware(
        RequestDelegate next,
        ICostManagementMetricsService metricsService,
        IServiceIdentifierExtractor serviceExtractor,
        ILogger<CostManagementMiddleware> logger)
    {
        _next = next;
        _metricsService = metricsService;
        _serviceExtractor = serviceExtractor;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? orgIdentifier = null;
        TransactionType? transactionType = null;

        try
        {
            // Determine transaction type from endpoint metadata (attribute-based)
            transactionType = ResolveTransactionTypeFromEndpointMetadata(context);

            // If we can't map to a transaction type, skip metrics
            if (!transactionType.HasValue)
            {
                await _next(context);
                return;
            }

            // Extract organization identifier from authenticated user claims
            try
            {
                orgIdentifier = _serviceExtractor.ExtractServiceIdentifier(context);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to extract organization identifier from user claims for {Method} {Path}",
                    context.Request.Method, context.Request.Path.Value);
                // Continue with null organization identifier
            }

            // Continue processing the request
            await _next(context);

            // Record the metric after successful processing
            _metricsService.RecordTransaction(
                transactionType.Value,
                context.Response.StatusCode,
                orgIdentifier);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to capture cost management metrics for {Method} {Path}. TransactionType: {TransactionType}, OrgIdentifier: {OrgIdentifier}",
                context.Request.Method,
                context.Request.Path.Value,
                transactionType?.ToString() ?? "Unknown",
                orgIdentifier ?? "null");

            // Continue processing even if metrics capture fails
            await _next(context);
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
        services.AddSingleton<IServiceIdentifierExtractor, ServiceIdentifierExtractor>();

        return services;
    }
}
