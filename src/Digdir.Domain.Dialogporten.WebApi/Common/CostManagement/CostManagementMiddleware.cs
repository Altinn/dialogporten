using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Context;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Middleware to automatically capture cost management metrics
/// </summary>
public sealed class CostManagementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICostManagementMetricsService _metricsService;
    private readonly ILogger<CostManagementMiddleware> _logger;
    private readonly CostManagementOptions _options;

    public CostManagementMiddleware(
        RequestDelegate next,
        ICostManagementMetricsService metricsService,
        ILogger<CostManagementMiddleware> logger,
        IOptions<CostManagementOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? tokenOrg = null;

        // If cost management is disabled, skip all processing
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

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
            _logger.LogWarning(ex, "Exception during token organization extraction for cost tracking - {Method} {Path}",
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
            var serviceOrg = applicationContext?.Metadata.TryGetValue(CostManagementMetadataKeys.ServiceOrg, out var orgValue) == true ? orgValue : null;
            var serviceResource = applicationContext?.Metadata.TryGetValue(CostManagementMetadataKeys.ServiceResource, out var srValue) == true ? srValue : null;

            // Log warning if metadata is missing but continue gracefully
            if (applicationContext == null)
            {
                _logger.LogWarning("IApplicationContext not available for cost management metrics on {Method} {Path}",
                    context.Request.Method, context.Request.Path.Value);
            }
            else if (string.IsNullOrEmpty(serviceOrg) || string.IsNullOrEmpty(serviceResource))
            {
                _logger.LogWarning("Missing service metadata from handler for cost tracking on {Method} {Path}: serviceOrg={ServiceOrg}, serviceResource={ServiceResource}",
                    context.Request.Method, context.Request.Path.Value, serviceOrg ?? "null", serviceResource ?? "null");
            }

            // Only enqueue billable outcomes (2xx, 4xx)
            var statusCode = context.Response.StatusCode;
            if (statusCode is not ((>= 200 and < 300) or (>= 400 and < 500)))
            {
                return;
            }

            // Queue the transaction for background processing (fire-and-forget)
            _metricsService.QueueTransaction(
                transactionType.Value,
                statusCode,
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
                transactionType.ToString(),
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
        // Register channel with deferred factory that resolves options at runtime
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<CostManagementOptions>>().Value;
            return System.Threading.Channels.Channel.CreateBounded<TransactionRecord>(options.QueueCapacity);
        });

        // Register channel reader/writer from channel
        services.AddSingleton(provider => provider.GetRequiredService<System.Threading.Channels.Channel<TransactionRecord>>().Reader);
        services.AddSingleton(provider => provider.GetRequiredService<System.Threading.Channels.Channel<TransactionRecord>>().Writer);

        // Register shared meter
        services.AddSingleton(_ => new System.Diagnostics.Metrics.Meter("Dialogporten.CostManagement", "1.0.0"));

        // Register cost management service
        services.AddSingleton<CostManagementService>(provider =>
        {
            var writer = provider.GetRequiredService<System.Threading.Channels.ChannelWriter<TransactionRecord>>();
            var reader = provider.GetRequiredService<System.Threading.Channels.ChannelReader<TransactionRecord>>();
            var logger = provider.GetRequiredService<ILogger<CostManagementService>>();
            var options = provider.GetRequiredService<IOptions<CostManagementOptions>>().Value;
            var hostEnvironment = provider.GetRequiredService<IHostEnvironment>();
            var meter = provider.GetRequiredService<System.Diagnostics.Metrics.Meter>();

            return new CostManagementService(writer, reader, logger, options, hostEnvironment, meter);
        });

        // Register service interface with enabled/disabled factory
        services.AddSingleton<ICostManagementMetricsService>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<CostManagementOptions>>().Value;
            return options.Enabled
                ? provider.GetRequiredService<CostManagementService>()
                : new NoOpCostManagementService();
        });


        // Register background service with enabled/disabled factory
        services.AddSingleton<IHostedService>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<CostManagementOptions>>().Value;
            return options.Enabled
                ? provider.GetRequiredService<CostManagementService>()
                : new NoOpHostedService();
        });

        return services;
    }
}
