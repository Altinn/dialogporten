namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Middleware to automatically capture cost management metrics
/// </summary>
public sealed class CostManagementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICostManagementMetricsService _metricsService;
    private readonly ITransactionTypeMapper _transactionMapper;
    private readonly IServiceIdentifierExtractor _serviceExtractor;
    private readonly ILogger<CostManagementMiddleware> _logger;

    public CostManagementMiddleware(
        RequestDelegate next,
        ICostManagementMetricsService metricsService,
        ITransactionTypeMapper transactionMapper,
        IServiceIdentifierExtractor serviceExtractor,
        ILogger<CostManagementMiddleware> logger)
    {
        _next = next;
        _metricsService = metricsService;
        _transactionMapper = transactionMapper;
        _serviceExtractor = serviceExtractor;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip metrics for health check endpoints and other non-dialog operations
        if (ShouldSkipMetrics(context.Request.Path))
        {
            await _next(context);
            return;
        }

        string? orgIdentifier = null;
        TransactionType? transactionType = null;

        try
        {
            // Determine transaction type
            var hasEndUserId = context.Request.Query.ContainsKey("enduserid");
            transactionType = _transactionMapper.GetTransactionType(
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty,
                hasEndUserId);

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

    private static bool ShouldSkipMetrics(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant();

        return pathValue switch
        {
            null => true,
            var p when p.StartsWith("/health", StringComparison.OrdinalIgnoreCase) => true,
            var p when p.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) => true,
            var p when p.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase) => true,
            var p when p.Contains("/wellknown", StringComparison.OrdinalIgnoreCase) => true,
            var p when !p.Contains("/api/v1/", StringComparison.OrdinalIgnoreCase) => true, // Only track v1 API calls
            _ => false
        };
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
        services.AddSingleton<ITransactionTypeMapper, TransactionTypeMapper>();
        services.AddSingleton<IServiceIdentifierExtractor, ServiceIdentifierExtractor>();

        return services;
    }
}
